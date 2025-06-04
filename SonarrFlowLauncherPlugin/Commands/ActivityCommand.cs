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
        public override string CommandDescription => "View current downloads and history (use: -a [q|queue|h|history])";

        public override List<Result> Execute(Query query)
        {
            if (!ValidateSettings())
            {
                return GetSettingsError();
            }

            var results = new List<Result>();
            try
            {
                // Clean up the query string: remove command flag, trim spaces, and convert to lowercase
                var searchQuery = query.Search
                    .Replace(CommandFlag, "", StringComparison.OrdinalIgnoreCase)
                    .Trim()
                    .ToLower();

                var activity = SonarrService.GetActivityAsync().Result;
                var totalItems = 0;

                // Show help if no specific filter
                if (string.IsNullOrWhiteSpace(searchQuery))
                {
                    results.Add(new Result
                    {
                        Title = "Activity Options",
                        SubTitle = "Type: q/queue (downloads in progress) or h/history (recent activity)",
                        IcoPath = "Images\\icon.png",
                        Score = 100
                    });
                }
                
                // Filter based on query
                switch (searchQuery)
                {
                    case "q":
                    case "queue":
                        // Add queue items
                        if (!activity.Queue.Any())
                        {
                            results.Add(new Result
                            {
                                Title = "No Active Downloads",
                                SubTitle = "Queue is empty",
                                IcoPath = "Images\\icon.png",
                                Score = 100
                            });
                        }
                        else
                        {
                            foreach (var item in activity.Queue)
                            {
                                results.Add(new Result
                                {
                                    Title = $"⬇️ {item.Title}",
                                    SubTitle = $"S{item.SeasonNumber:D2}E{item.EpisodeNumber:D2} - {item.Status} ({item.Progress:F1}%) - {item.Quality} | Right-click for options",
                                    IcoPath = !string.IsNullOrEmpty(item.PosterPath) ? item.PosterPath : "Images\\icon.png",
                                    Score = 100 - totalItems,
                                    ContextData = item,
                                    Action = _ => false
                                });
                                totalItems++;
                            }
                        }
                        break;

                    case "h":
                    case "history":
                        // Add history items
                        if (!activity.History.Any())
                        {
                            results.Add(new Result
                            {
                                Title = "No Recent Activity",
                                SubTitle = "History is empty",
                                IcoPath = "Images\\icon.png",
                                Score = 100
                            });
                        }
                        else
                        {
                            foreach (var item in activity.History)
                            {
                                var icon = item.EventType.ToLower() switch
                                {
                                    "grabbed" => "⬇️",
                                    "downloadfolderimported" => "✅",
                                    "downloadfailed" => "❌",
                                    _ => "📝"
                                };

                                string episodeInfo = $"S{item.SeasonNumber:D2}E{item.EpisodeNumber:D2}";

                                results.Add(new Result
                                {
                                    Title = $"{icon} {item.Title} - {episodeInfo}",
                                    SubTitle = $"{item.EventType} - {item.Quality} - {item.Date:g} | Right-click for options",
                                    IcoPath = !string.IsNullOrEmpty(item.PosterPath) ? item.PosterPath : "Images\\icon.png",
                                    Score = 100 - totalItems,
                                    ContextData = item,
                                    Action = _ => false
                                });
                                totalItems++;
                            }
                        }
                        break;

                    default:
                        // If no specific filter or unknown filter, show both queue and history
                        // Add queue items (prioritize these)
                        foreach (var item in activity.Queue)
                        {
                            results.Add(new Result
                            {
                                Title = $"⬇️ {item.Title}",
                                SubTitle = $"S{item.SeasonNumber:D2}E{item.EpisodeNumber:D2} - {item.Status} ({item.Progress:F1}%) - {item.Quality} | Right-click for options",
                                IcoPath = !string.IsNullOrEmpty(item.PosterPath) ? item.PosterPath : "Images\\icon.png",
                                Score = 100 - totalItems,
                                ContextData = item,
                                Action = _ => false
                            });
                            totalItems++;
                        }

                        // Add history items (fill remaining slots)
                        foreach (var item in activity.History)
                        {
                            var icon = item.EventType.ToLower() switch
                            {
                                "grabbed" => "⬇️",
                                "downloadfolderimported" => "✅",
                                "downloadfailed" => "❌",
                                _ => "📝"
                            };

                            string episodeInfo = $"S{item.SeasonNumber:D2}E{item.EpisodeNumber:D2}";

                            results.Add(new Result
                            {
                                Title = $"{icon} {item.Title} - {episodeInfo}",
                                SubTitle = $"{item.EventType} - {item.Quality} - {item.Date:g} | Right-click for options",
                                IcoPath = !string.IsNullOrEmpty(item.PosterPath) ? item.PosterPath : "Images\\icon.png",
                                Score = 95 - totalItems,
                                ContextData = item,
                                Action = _ => false
                            });
                            totalItems++;
                        }
                        break;
                }

                if (!results.Any())
                {
                    results.Add(new Result
                    {
                        Title = "No Activity Found",
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
                    Action = _ => SonarrService.OpenActivityInBrowser()
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