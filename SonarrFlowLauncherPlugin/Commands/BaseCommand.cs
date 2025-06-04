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
                    Title = "🔧 Setup Required: Sonarr API Key Not Set",
                    SubTitle = "Type 'snr -setup' to start guided setup wizard",
                    IcoPath = "Images\\icon.png",
                    Score = 100,
                    Action = _ => false
                },
                new Result
                {
                    Title = "⚙️ Alternative: Plugin Settings Panel",
                    SubTitle = "Open Flow Launcher Settings → Plugins → Sonarr Explorer to configure manually",
                    IcoPath = "Images\\icon.png",
                    Score = 95,
                    Action = _ => false
                },
                new Result
                {
                    Title = "❓ How to Find Your API Key",
                    SubTitle = "In Sonarr: Settings → General → API Key (copy the long string)",
                    IcoPath = "Images\\icon.png",
                    Score = 90,
                    Action = _ => false
                },
                new Result
                {
                    Title = "📖 Quick Start Guide",
                    SubTitle = "1. Get API key from Sonarr → 2. Type 'snr -setup' → 3. Follow wizard",
                    IcoPath = "Images\\icon.png",
                    Score = 85,
                    Action = _ => false
                }
            };
        }

        private bool OpenPluginSettings()
        {
            // Just return false since we can't reliably open Flow Launcher settings
            // The user will need to open it manually
            return false;
        }
    }
} 