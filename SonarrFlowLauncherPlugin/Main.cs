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
        private readonly Settings _settings;
        private readonly SonarrService _sonarrService;
        private readonly SettingsControl _settingsControl;
        private readonly CommandManager _commandManager;

        public Main()
        {
            _settings = Settings.Load();
            _sonarrService = new SonarrService(_settings);
            _settingsControl = new SettingsControl(_settings);
            _commandManager = new CommandManager(_sonarrService, _settings);
        }

        public void Init(PluginInitContext context)
        {
            _context = context;
        }

        public List<Result> Query(Query query)
        {
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