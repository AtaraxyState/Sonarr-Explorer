using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Services;

namespace SonarrFlowLauncherPlugin.Commands
{
    public class HelpCommand : BaseCommand
    {
        public HelpCommand(SonarrService sonarrService, Settings settings) 
            : base(sonarrService, settings)
        {
        }

        public override string CommandFlag => "-help";
        public override string CommandName => "Show Help";
        public override string CommandDescription => "Display all available commands and usage information";

        public override List<Result> Execute(Query query)
        {
            var results = new List<Result>();

            // Main help header
            results.Add(new Result
            {
                Title = "🔍 Sonarr Flow Launcher Plugin Help",
                SubTitle = "Available commands and usage information",
                IcoPath = "Images\\icon.png",
                Score = 100
            });

            // API-based commands
            results.Add(new Result
            {
                Title = "📅 Calendar Commands",
                SubTitle = "snr -c [today|tomorrow|week|next week|month] - View upcoming episodes",
                IcoPath = "Images\\icon.png",
                Score = 95
            });

            results.Add(new Result
            {
                Title = "📊 Activity Commands", 
                SubTitle = "snr -a [q|queue|h|history] - View downloads and activity",
                IcoPath = "Images\\icon.png",
                Score = 94
            });

            results.Add(new Result
            {
                Title = "🔍 Library Search",
                SubTitle = "snr -l [search term] - Search your Sonarr library",
                IcoPath = "Images\\icon.png",
                Score = 93
            });

            results.Add(new Result
            {
                Title = "🏥 System Health",
                SubTitle = "snr -s - Monitor health checks, view issues, and trigger re-tests",
                IcoPath = "Images\\icon.png",
                Score = 92
            });

            // Offline/utility commands
            results.Add(new Result
            {
                Title = "ℹ️ Information Commands",
                SubTitle = "snr -about - Plugin information and version",
                IcoPath = "Images\\icon.png",
                Score = 91
            });

            results.Add(new Result
            {
                Title = "🔧 Utility Commands",
                SubTitle = "snr -test - Test connection | snr -settings - Open settings",
                IcoPath = "Images\\icon.png",
                Score = 90
            });

            results.Add(new Result
            {
                Title = "🕒 Date/Time Helpers",
                SubTitle = "snr -date - Current date/time | snr -time [timezone] - Time conversion",
                IcoPath = "Images\\icon.png",
                Score = 89
            });

            results.Add(new Result
            {
                Title = "🔗 External Links",
                SubTitle = "snr -tvdb [series] - TVDB search | snr -imdb [series] - IMDB search",
                IcoPath = "Images\\icon.png",
                Score = 88
            });

            results.Add(new Result
            {
                Title = "🌐 Community Links",
                SubTitle = "snr -reddit - Open r/sonarr | snr -docs - Plugin documentation",
                IcoPath = "Images\\icon.png",
                Score = 87
            });

            // Configuration note
            if (string.IsNullOrEmpty(Settings.ApiKey))
            {
                results.Add(new Result
                {
                    Title = "⚠️ Setup Required",
                    SubTitle = "Configure API key in settings to use calendar, activity, and library features",
                    IcoPath = "Images\\icon.png",
                    Score = 100
                });
            }

            return results;
        }
    }
} 