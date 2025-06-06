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
    /// <summary>
    /// Main plugin class that serves as the entry point for the Sonarr Flow Launcher plugin.
    /// Implements Flow Launcher interfaces for plugin functionality, settings management, and context menus.
    /// Provides integration with Sonarr API for series management, episode tracking, and system controls.
    /// </summary>
    /// <remarks>
    /// This plugin allows users to:
    /// - Search and browse Sonarr series library
    /// - View calendar events and upcoming episodes
    /// - Monitor download activity and queue status
    /// - Refresh series and trigger rescans
    /// - Access series folders and episode files directly
    /// - Manage Sonarr through various utility commands
    /// </remarks>
    public class Main : IPlugin, ISettingProvider, IContextMenu
    {
        /// <summary>
        /// Flow Launcher plugin initialization context containing metadata and API access
        /// </summary>
        private PluginInitContext _context;
        
        /// <summary>
        /// Plugin settings instance containing Sonarr API configuration and user preferences
        /// </summary>
        private Settings _settings;
        
        /// <summary>
        /// Service for communicating with Sonarr API endpoints
        /// </summary>
        private SonarrService _sonarrService;
        
        /// <summary>
        /// WPF user control for plugin settings management interface
        /// </summary>
        private SettingsControl _settingsControl;
        
        /// <summary>
        /// Manager for handling different plugin commands and routing queries
        /// </summary>
        private CommandManager _commandManager;
        
        /// <summary>
        /// Service for building context menus for series and episode items
        /// </summary>
        private Services.ContextMenuService _contextMenuService;
        
        /// <summary>
        /// Timestamp of last settings file modification check for hot-reloading
        /// </summary>
        private DateTime _lastSettingsCheck = DateTime.MinValue;
        
        /// <summary>
        /// Current Sonarr API connection status. null = not tested, true = connected, false = failed
        /// </summary>
        private bool? _connectionStatus = null;
        
        /// <summary>
        /// Last connection error message if connection failed
        /// </summary>
        private string _connectionError = string.Empty;
        
        /// <summary>
        /// Timestamp of last connection test to prevent excessive API calls
        /// </summary>
        private DateTime _lastConnectionTest = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the Main plugin class and sets up all required services
        /// </summary>
        public Main()
        {
            InitializeServices();
        }

        /// <summary>
        /// Initializes or re-initializes all plugin services with current settings.
        /// Creates fresh instances of SonarrService, ContextMenuService, and CommandManager.
        /// </summary>
        /// <remarks>
        /// This method is called during plugin startup and when settings are changed.
        /// It properly disposes of existing services before creating new ones.
        /// </remarks>
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

        /// <summary>
        /// Refreshes only the service layer components without touching UI elements.
        /// Safe to call from background threads when settings have changed.
        /// </summary>
        /// <remarks>
        /// This method is used for hot-reloading settings changes without recreating UI components.
        /// It resets connection status and automatically tests the new connection.
        /// </remarks>
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

        /// <summary>
        /// Monitors the plugin settings file for changes and automatically reloads services if modified.
        /// Implements hot-reloading functionality for seamless settings updates.
        /// </summary>
        /// <remarks>
        /// This method is called before each query to ensure the plugin uses the latest settings.
        /// It safely handles file system errors and background thread execution.
        /// </remarks>
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

        /// <summary>
        /// Initializes the plugin with Flow Launcher context and performs initial connection testing.
        /// Called by Flow Launcher when the plugin is first loaded.
        /// </summary>
        /// <param name="context">Flow Launcher plugin initialization context containing metadata and APIs</param>
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

        /// <summary>
        /// Processes user queries and returns relevant results based on input commands.
        /// Main entry point for all plugin functionality including search, commands, and status display.
        /// </summary>
        /// <param name="query">User query containing search terms and command flags</param>
        /// <returns>List of results to display in Flow Launcher including actions and context data</returns>
        /// <remarks>
        /// This method:
        /// - Checks for settings changes before processing
        /// - Shows connection status warnings when API is unreachable
        /// - Routes queries to appropriate command handlers
        /// - Provides fallback results for connection issues
        /// </remarks>
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

        /// <summary>
        /// Creates and returns the WPF settings panel for plugin configuration.
        /// Called by Flow Launcher when user accesses plugin settings.
        /// </summary>
        /// <returns>WPF UserControl containing settings interface</returns>
        /// <remarks>
        /// Always creates a fresh settings control with the latest settings to ensure UI consistency.
        /// This method is called on the UI thread so it's safe to create WPF controls.
        /// </remarks>
        public Control CreateSettingPanel()
        {
            // This method is called on the UI thread, so it's safe to create/recreate the settings control here
            // Always create a fresh settings control with latest settings when the settings panel is opened
            var latestSettings = Settings.Load();
            _settingsControl = new SettingsControl(latestSettings);
            _settings = latestSettings; // Update our reference too
            return _settingsControl;
        }

        /// <summary>
        /// Builds context menu options for selected results based on their type and available data.
        /// Provides file access, refresh options, and series-specific actions.
        /// </summary>
        /// <param name="selectedResult">The result item that was right-clicked to show context menu</param>
        /// <returns>List of context menu options with actions for the selected item</returns>
        /// <remarks>
        /// Supports context menus for:
        /// - Series results: folder access, episode files, refresh options
        /// - Episode results: specific episode files, series actions, episode details
        /// - Health check results: re-test options, system status access
        /// - Uses ContextData property to determine result type and available actions
        /// </remarks>
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
            // Check if this is a health check item (system command)
            else if (selectedResult.ContextData is SonarrHealthCheck healthCheck)
            {
                return Commands.SystemCommand.CreateHealthCheckContextMenu(healthCheck, _sonarrService);
            }

            return new List<Result>();
        }

        /// <summary>
        /// Performs asynchronous connection testing to Sonarr API with rate limiting.
        /// Tests connectivity by attempting to retrieve series list and updates connection status.
        /// </summary>
        /// <remarks>
        /// This method:
        /// - Implements rate limiting (max once per minute) to prevent API spam
        /// - Runs on background thread to avoid blocking UI
        /// - Updates connection status and error message fields
        /// - Provides detailed logging for troubleshooting
        /// </remarks>
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