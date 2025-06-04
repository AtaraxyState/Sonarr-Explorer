using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Commands;
using SonarrFlowLauncherPlugin.Services;
using SonarrFlowLauncherPlugin.Models;
using System.Diagnostics;
using System.Linq;

namespace SonarrFlowLauncherPlugin
{
    public class Main : IPlugin, ISettingProvider, IContextMenu
    {
        private PluginInitContext _context;
        private Settings _settings;
        private SonarrService _sonarrService;
        private SettingsControl _settingsControl;
        private CommandManager _commandManager;
        private Services.ContextMenuService _contextMenuService;
        private DateTime _lastSettingsCheck = DateTime.MinValue;
        
        // Connection status tracking
        private bool? _connectionStatus = null; // null = not tested, true = ok, false = failed
        private string _connectionError = string.Empty;
        private DateTime _lastConnectionTest = DateTime.MinValue;

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
            
            // Create context menu service
            _contextMenuService = new Services.ContextMenuService(_sonarrService);
            
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
            
            // Create context menu service
            _contextMenuService = new Services.ContextMenuService(_sonarrService);
            
            // Create new command manager with updated services and context
            _commandManager = new CommandManager(_sonarrService, latestSettings, _context);
            
            // Update the settings reference
            _settings = latestSettings;
            
            // Reset connection status and test with new settings
            _connectionStatus = null;
            _connectionError = string.Empty;
            _lastConnectionTest = DateTime.MinValue;
            
            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                System.Diagnostics.Debug.WriteLine("Settings changed - testing new connection...");
                TestConnectionAsync();
            }
            
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
            
            // Perform automatic connection test if API key is configured
            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                System.Diagnostics.Debug.WriteLine("Plugin initialized - starting automatic connection test...");
                TestConnectionAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Plugin initialized - no API key configured, skipping connection test");
                _connectionStatus = false;
                _connectionError = "API key not configured";
            }
        }

        public List<Result> Query(Query query)
        {
            // Check for settings changes before processing query
            CheckForSettingsChanges();
            
            bool hasApiKey = !string.IsNullOrEmpty(_settings.ApiKey);
            
            // Show connection status if there are issues and this is not a specific command
            if (hasApiKey && !string.IsNullOrEmpty(query.Search) && _connectionStatus == false && 
                !query.Search.StartsWith("-"))
            {
                var connectionResults = new List<Result>
                {
                    new Result
                    {
                        Title = "⚠️ Sonarr Connection Issue",
                        SubTitle = $"Error: {_connectionError} | Click to test connection",
                        IcoPath = "Images\\icon.png",
                        Score = 1000,
                        Action = _ => {
                            TestConnectionAsync();
                            return true;
                        }
                    },
                    new Result
                    {
                        Title = "🔧 Open Plugin Settings",
                        SubTitle = "Configure Sonarr API settings",
                        IcoPath = "Images\\icon.png",
                        Score = 999,
                        Action = _ => false
                    }
                };
                
                // Add regular results below connection status
                var regularResults = _commandManager.HandleQuery(query, hasApiKey);
                connectionResults.AddRange(regularResults);
                return connectionResults;
            }
            
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
            // Check if this is a series result from library search
            if (selectedResult.ContextData is SonarrSeries series)
            {
                return _contextMenuService.BuildSeriesContextMenu(series);
            }
            // Check if this is an episode item (calendar/activity)
            else if (selectedResult.ContextData is SonarrEpisodeBase episodeItem)
            {
                return _contextMenuService.BuildEpisodeContextMenu(episodeItem);
            }

            return new List<Result>();
        }

        private void TestConnectionAsync()
        {
            // Don't test too frequently (max once per minute)
            if (DateTime.Now - _lastConnectionTest < TimeSpan.FromMinutes(1))
            {
                System.Diagnostics.Debug.WriteLine("Skipping connection test - tested recently");
                return;
            }
            
            _lastConnectionTest = DateTime.Now;
            
            // Run connection test in background
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Testing connection to Sonarr...");
                    
                    // Try to get series list to test connection
                    var series = await _sonarrService.SearchSeriesAsync("");
                    
                    _connectionStatus = true;
                    _connectionError = string.Empty;
                    System.Diagnostics.Debug.WriteLine($"Connection test successful - found {series.Count} series");
                }
                catch (Exception ex)
                {
                    _connectionStatus = false;
                    _connectionError = ex.InnerException?.Message ?? ex.Message;
                    System.Diagnostics.Debug.WriteLine($"Connection test failed: {_connectionError}");
                }
            });
        }
    }
} 