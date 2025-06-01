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
            
            // Create settings control
            _settingsControl = new SettingsControl(_settings);
            
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
                    // Settings have changed, reinitialize services
                    InitializeServices();
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
            return _settingsControl;
        }
    }
} 