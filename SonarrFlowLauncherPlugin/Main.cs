using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Commands;
using SonarrFlowLauncherPlugin.Services;

namespace SonarrFlowLauncherPlugin
{
    public class Main : IPlugin, ISettingProvider
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
            
            // Create new command manager with updated services
            _commandManager = new CommandManager(_sonarrService, _settings);
            
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
            
            // Create new command manager with updated services
            _commandManager = new CommandManager(_sonarrService, latestSettings);
            
            // Update the settings reference
            _settings = latestSettings;
            
            _lastSettingsCheck = DateTime.Now;
        }

        private void CheckForSettingsChanges()
        {
            // Check if settings file has been modified since last check
            var settingsPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(typeof(Settings).Assembly.Location), 
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

        public void Init(PluginInitContext context)
        {
            _context = context;
        }

        public List<Result> Query(Query query)
        {
            // Check for settings changes before processing query
            CheckForSettingsChanges();
            
            if (string.IsNullOrEmpty(_settings.ApiKey))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Sonarr API Key Not Set",
                        SubTitle = "Please set your Sonarr API key in the plugin settings",
                        IcoPath = "Images\\icon.png",
                        Action = _ => false
                    }
                };
            }

            return _commandManager.HandleQuery(query);
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
    }
} 