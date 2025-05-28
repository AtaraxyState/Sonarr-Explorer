using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Services;

namespace SonarrFlowLauncherPlugin.Commands
{
    public class ActivityCommand : BaseCommand
    {
        public ActivityCommand(SonarrService sonarrService, Settings settings) 
            : base(sonarrService, settings)
        {
        }

        public override string CommandFlag => "-a";
        public override string CommandName => "View Sonarr Activity";
        public override string CommandDescription => "View current downloads and history";

        public override List<Result> Execute(Query query)
        {
            if (!ValidateSettings())
            {
                return GetSettingsError();
            }

            var results = new List<Result>();
            try
            {
                var activity = SonarrService.GetActivityAsync().Result;
                var totalItems = 0;
                const int maxItems = 10;
                
                // Add queue items (prioritize these)
                foreach (var item in activity.Queue)
                {
                    if (totalItems >= maxItems) break;
                    
                    results.Add(new Result
                    {
                        Title = $"⬇️ {item.Title}",
                        SubTitle = $"S{item.SeasonNumber:D2}E{item.EpisodeNumber:D2} - {item.Status} ({item.Progress:F1}%) - {item.Quality}",
                        IcoPath = !string.IsNullOrEmpty(item.PosterPath) ? item.PosterPath : "Images\\icon.png",
                        Score = 100
                    });
                    totalItems++;
                }

                // Add history items (fill remaining slots)
                foreach (var item in activity.History)
                {
                    if (totalItems >= maxItems) break;
                    
                    var icon = item.EventType.ToLower() switch
                    {
                        "grabbed" => "⬇️",
                        "downloadfolderimported" => "✅",
                        "downloadfailed" => "❌",
                        _ => "📝"
                    };

                    results.Add(new Result
                    {
                        Title = $"📜 {item.Title}",
                        SubTitle = $"S{item.SeasonNumber:D2}E{item.EpisodeNumber:D2} - {item.EventType} - {item.Quality} - {item.Date:g}",
                        IcoPath = !string.IsNullOrEmpty(item.PosterPath) ? item.PosterPath : "Images\\icon.png",
                        Score = 95
                    });
                    totalItems++;
                }

                if (!results.Any())
                {
                    results.Add(new Result
                    {
                        Title = "No Recent Activity",
                        SubTitle = "No downloads in progress or recent history",
                        IcoPath = "Images\\icon.png",
                        Score = 100
                    });
                }

                // Add option to open in browser
                results.Add(new Result
                {
                    Title = "Open Activity in Browser",
                    SubTitle = "View full activity in Sonarr",
                    IcoPath = "Images\\icon.png",
                    Score = 80,
                    Action = _ => SonarrService.OpenActivityInBrowser().Result
                });

                return results;
            }
            catch (Exception ex)
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Error Getting Activity",
                        SubTitle = $"Error: {ex.Message}",
                        IcoPath = "Images\\icon.png",
                        Score = 100
                    }
                };
            }
        }
    }
} 