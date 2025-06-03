using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Services;

namespace SonarrFlowLauncherPlugin.Commands
{
    public abstract class BaseCommand
    {
        protected readonly SonarrService SonarrService;
        protected readonly Settings Settings;

        protected BaseCommand(SonarrService sonarrService, Settings settings)
        {
            SonarrService = sonarrService;
            Settings = settings;
        }

        public abstract string CommandFlag { get; }
        public abstract string CommandName { get; }
        public abstract string CommandDescription { get; }
        
        public abstract List<Result> Execute(Query query);

        // Public property to access SonarrService from CommandManager
        public SonarrService GetSonarrService() => SonarrService;

        protected bool ValidateSettings()
        {
            if (string.IsNullOrEmpty(Settings.ApiKey))
            {
                return false;
            }
            return true;
        }

        protected List<Result> GetSettingsError()
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
    }
} 