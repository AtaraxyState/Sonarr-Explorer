using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Commands;
using SonarrFlowLauncherPlugin.Services;
using SonarrFlowLauncherPlugin.Models;
using System.Diagnostics;

namespace SonarrFlowLauncherPlugin
{
    public class Main : IPlugin, ISettingProvider, IContextMenu
    {
        private PluginInitContext _context;
        private Settings _settings;
        private SonarrService _sonarrService;
        private SettingsControl _settingsControl;
        private CommandManager _commandManager;
        private DateTime _lastSettingsCheck = DateTime.MinValue;

        public Main()
        {
            InitializeServices();
        }

        private void InitializeServices()
        {
            // Dispose existing service if it exists
            _sonarrService?.Dispose();
            
            // Load fresh settings
            _settings = Settings.Load();
            
            // Create new service with updated settings
            _sonarrService = new SonarrService(_settings);
            
            // Create new command manager with updated services and context
            _commandManager = new CommandManager(_sonarrService, _settings, _context);
            
            // Only create settings control if we don't have one yet
            // (avoid recreating UI components on background threads)
            if (_settingsControl == null)
            {
                _settingsControl = new SettingsControl(_settings);
            }
            
            _lastSettingsCheck = DateTime.Now;
        }

        private void RefreshServicesOnly()
        {
            // Dispose existing service if it exists
            _sonarrService?.Dispose();
            
            // Load fresh settings (don't assign to _settings yet, just get latest values)
            var latestSettings = Settings.Load();
            
            // Create new service with updated settings
            _sonarrService = new SonarrService(latestSettings);
            
            // Create new command manager with updated services and context
            _commandManager = new CommandManager(_sonarrService, latestSettings, _context);
            
            // Update the settings reference
            _settings = latestSettings;
            
            _lastSettingsCheck = DateTime.Now;
        }

        private void CheckForSettingsChanges()
        {
            try
            {
                // Check if settings file has been modified since last check
                var settingsPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(typeof(Settings).Assembly.Location) ?? "",
                    "plugin.yaml");
                    
                if (System.IO.File.Exists(settingsPath))
                {
                    var lastWrite = System.IO.File.GetLastWriteTime(settingsPath);
                    if (lastWrite > _lastSettingsCheck)
                    {
                        // Settings have changed, but we're likely on a background thread
                        // Only refresh the services (no UI components)
                        RefreshServicesOnly();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the plugin
                System.Diagnostics.Debug.WriteLine($"Error checking settings changes: {ex.Message}");
            }
        }

        public void Init(PluginInitContext context)
        {
            _context = context;
        }

        public List<Result> Query(Query query)
        {
            // Check for settings changes before processing query
            CheckForSettingsChanges();
            
            bool hasApiKey = !string.IsNullOrEmpty(_settings.ApiKey);
            
            return _commandManager.HandleQuery(query, hasApiKey);
        }

        public Control CreateSettingPanel()
        {
            // This method is called on the UI thread, so it's safe to create/recreate the settings control here
            // Always create a fresh settings control with latest settings when the settings panel is opened
            var latestSettings = Settings.Load();
            _settingsControl = new SettingsControl(latestSettings);
            _settings = latestSettings; // Update our reference too
            return _settingsControl;
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var contextMenus = new List<Result>();

            // Check if this is a series result from library search
            if (selectedResult.ContextData is SonarrSeries series)
            {
                // Add option to open local folder if path exists - this should be the primary option
                if (!string.IsNullOrEmpty(series.Path) && System.IO.Directory.Exists(series.Path))
                {
                    contextMenus.Add(new Result
                    {
                        Title = "📁 Open Series Folder",
                        SubTitle = $"Open {series.Path} in Windows Explorer",
                        IcoPath = "Images\\icon.png",
                        Score = 100,
                        Action = _ => OpenFolderInExplorer(series.Path)
                    });
                }

                // Add option to open in Sonarr web UI
                contextMenus.Add(new Result
                {
                    Title = "🌐 Open in Sonarr",
                    SubTitle = "Open series page in Sonarr web interface",
                    IcoPath = "Images\\icon.png",
                    Score = 95,
                    Action = _ => _sonarrService.OpenSeriesInBrowser(series.TitleSlug)
                });

                // Add option to refresh series
                contextMenus.Add(new Result
                {
                    Title = "🔄 Refresh Series",
                    SubTitle = "Trigger a rescan of this series",
                    IcoPath = "Images\\icon.png",
                    Score = 90,
                    Action = _ => RefreshSeries(series)
                });

                // Add copy path option if path exists
                if (!string.IsNullOrEmpty(series.Path))
                {
                    contextMenus.Add(new Result
                    {
                        Title = "📋 Copy Path",
                        SubTitle = $"Copy path to clipboard: {series.Path}",
                        IcoPath = "Images\\icon.png",
                        Score = 85,
                        Action = _ => CopyToClipboard(series.Path)
                    });
                }
            }
            // Check if this is an activity item (queue or history)
            else if (selectedResult.ContextData is SonarrEpisodeBase episodeItem)
            {
                var seriesTitle = !string.IsNullOrEmpty(episodeItem.SeriesTitle) ? episodeItem.SeriesTitle : episodeItem.Title;
                
                // Add option to open episode file with default application
                if (!string.IsNullOrEmpty(episodeItem.EpisodeFilePath) && System.IO.File.Exists(episodeItem.EpisodeFilePath))
                {
                    contextMenus.Add(new Result
                    {
                        Title = "▶️ Open Episode File",
                        SubTitle = $"Open {System.IO.Path.GetFileName(episodeItem.EpisodeFilePath)} with default application",
                        IcoPath = "Images\\icon.png",
                        Score = 105,
                        Action = _ => OpenFileWithDefaultApp(episodeItem.EpisodeFilePath)
                    });
                }
                
                // Add option to open local folder if path exists
                if (!string.IsNullOrEmpty(episodeItem.SeriesPath) && System.IO.Directory.Exists(episodeItem.SeriesPath))
                {
                    contextMenus.Add(new Result
                    {
                        Title = "📁 Open Series Folder",
                        SubTitle = $"Open {episodeItem.SeriesPath} in Windows Explorer",
                        IcoPath = "Images\\icon.png",
                        Score = 100,
                        Action = _ => OpenFolderInExplorer(episodeItem.SeriesPath)
                    });
                }

                // Add option to open series in Sonarr web UI
                if (!string.IsNullOrEmpty(episodeItem.TitleSlug))
                {
                    contextMenus.Add(new Result
                    {
                        Title = "🌐 Open Series in Sonarr",
                        SubTitle = "Open series page in Sonarr web interface",
                        IcoPath = "Images\\icon.png",
                        Score = 95,
                        Action = _ => _sonarrService.OpenSeriesInBrowser(episodeItem.TitleSlug)
                    });
                }

                // Add option to refresh series
                if (episodeItem.SeriesId > 0)
                {
                    contextMenus.Add(new Result
                    {
                        Title = "🔄 Refresh Series",
                        SubTitle = $"Trigger a rescan of {seriesTitle}",
                        IcoPath = "Images\\icon.png",
                        Score = 90,
                        Action = _ => RefreshSeriesByEpisode(episodeItem)
                    });
                }

                // Add copy episode file path option if file path exists
                if (!string.IsNullOrEmpty(episodeItem.EpisodeFilePath))
                {
                    contextMenus.Add(new Result
                    {
                        Title = "📋 Copy Episode File Path",
                        SubTitle = $"Copy episode file path to clipboard: {episodeItem.EpisodeFilePath}",
                        IcoPath = "Images\\icon.png",
                        Score = 87,
                        Action = _ => CopyToClipboard(episodeItem.EpisodeFilePath)
                    });
                }

                // Add copy series path option if path exists
                if (!string.IsNullOrEmpty(episodeItem.SeriesPath))
                {
                    contextMenus.Add(new Result
                    {
                        Title = "📋 Copy Series Path",
                        SubTitle = $"Copy series path to clipboard: {episodeItem.SeriesPath}",
                        IcoPath = "Images\\icon.png",
                        Score = 85,
                        Action = _ => CopyToClipboard(episodeItem.SeriesPath)
                    });
                }

                // Add episode-specific options for activity items
                if (episodeItem is SonarrQueueItem queueItem)
                {
                    contextMenus.Add(new Result
                    {
                        Title = "📊 Download Progress",
                        SubTitle = $"Progress: {queueItem.Progress:F1}% - Status: {queueItem.Status}",
                        IcoPath = "Images\\icon.png",
                        Score = 80,
                        Action = _ => false // Just informational
                    });
                }
                else if (episodeItem is SonarrHistoryItem historyItem)
                {
                    contextMenus.Add(new Result
                    {
                        Title = "📅 Event Details",
                        SubTitle = $"Event: {historyItem.EventType} - Date: {historyItem.Date:g}",
                        IcoPath = "Images\\icon.png",
                        Score = 80,
                        Action = _ => false // Just informational
                    });
                }
            }

            return contextMenus;
        }

        private bool OpenFolderInExplorer(string path)
        {
            try
            {
                if (System.IO.Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{path}\"",
                        UseShellExecute = true
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening folder: {ex.Message}");
            }
            return false;
        }

        private bool RefreshSeries(SonarrSeries series)
        {
            try
            {
                // Run refresh in background to avoid blocking UI
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        await _sonarrService.RefreshSeriesAsync(series.Id);
                        System.Diagnostics.Debug.WriteLine($"Successfully refreshed series: {series.Title}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to refresh series {series.Title}: {ex.Message}");
                    }
                });
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting refresh for series {series.Title}: {ex.Message}");
                return false;
            }
        }

        private bool RefreshSeriesByEpisode(SonarrEpisodeBase episodeItem)
        {
            try
            {
                // Run refresh in background to avoid blocking UI
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        await _sonarrService.RefreshSeriesAsync(episodeItem.SeriesId);
                        var seriesTitle = !string.IsNullOrEmpty(episodeItem.SeriesTitle) ? episodeItem.SeriesTitle : episodeItem.Title;
                        System.Diagnostics.Debug.WriteLine($"Successfully refreshed series: {seriesTitle}");
                    }
                    catch (Exception ex)
                    {
                        var seriesTitle = !string.IsNullOrEmpty(episodeItem.SeriesTitle) ? episodeItem.SeriesTitle : episodeItem.Title;
                        System.Diagnostics.Debug.WriteLine($"Failed to refresh series {seriesTitle}: {ex.Message}");
                    }
                });
                return true;
            }
            catch (Exception ex)
            {
                var seriesTitle = !string.IsNullOrEmpty(episodeItem.SeriesTitle) ? episodeItem.SeriesTitle : episodeItem.Title;
                System.Diagnostics.Debug.WriteLine($"Error starting refresh for series {seriesTitle}: {ex.Message}");
                return false;
            }
        }

        private bool CopyToClipboard(string text)
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error copying to clipboard: {ex.Message}");
                return false;
            }
        }

        private bool OpenFileWithDefaultApp(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening file: {ex.Message}");
            }
            return false;
        }
    }
} 