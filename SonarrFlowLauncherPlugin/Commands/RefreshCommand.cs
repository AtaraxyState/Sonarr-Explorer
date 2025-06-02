using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Services;

namespace SonarrFlowLauncherPlugin.Commands
{
    public class RefreshCommand : BaseCommand
    {
        public RefreshCommand(SonarrService sonarrService, Settings settings) 
            : base(sonarrService, settings)
        {
        }

        public override string CommandFlag => "-r";
        public override string CommandName => "Refresh Sonarr Series";
        public override string CommandDescription => "Refresh all series or search for a specific series to refresh (use: -r [all|series name])";

        public override List<Result> Execute(Query query)
        {
            if (!ValidateSettings())
            {
                return GetSettingsError();
            }

            var results = new List<Result>();
            
            // Clean up the query string: remove command flag, trim spaces, and convert to lowercase
            var searchQuery = query.Search
                .Replace(CommandFlag, "", StringComparison.OrdinalIgnoreCase)
                .Trim()
                .ToLower();

            System.Diagnostics.Debug.WriteLine($"Refresh command received. Raw query: '{query.Search}'");
            System.Diagnostics.Debug.WriteLine($"Cleaned search query: '{searchQuery}'");

            try
            {
                // If no search query or "all", show refresh all option
                if (string.IsNullOrWhiteSpace(searchQuery) || searchQuery == "all")
                {
                    results.Add(new Result
                    {
                        Title = "Refresh All Series",
                        SubTitle = "Trigger a refresh/rescan of all series in Sonarr",
                        IcoPath = "Images\\icon.png",
                        Score = 100,
                        Action = _ =>
                        {
                            // Use Task.Run to avoid blocking UI thread
                            Task.Run(async () =>
                            {
                                try
                                {
                                    await SonarrService.RefreshAllSeriesAsync();
                                    System.Diagnostics.Debug.WriteLine("Refresh all series command sent successfully");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Failed to refresh all series: {ex.Message}");
                                }
                            });
                            return true; // Return immediately, don't wait for completion
                        }
                    });

                    // If no search query, also show options
                    if (string.IsNullOrWhiteSpace(searchQuery))
                    {
                        results.Add(new Result
                        {
                            Title = "Refresh Options",
                            SubTitle = "Use '-r all' to refresh all series, or '-r <series name>' to search and refresh specific series",
                            IcoPath = "Images\\icon.png",
                            Score = 90,
                            Action = _ => false
                        });
                    }
                }
                else
                {
                    // Search for specific series to refresh
                    // Use Task.Run to avoid blocking UI thread during search
                    try
                    {
                        var series = Task.Run(async () => await SonarrService.SearchSeriesAsync(searchQuery)).Result;

                        if (series.Any())
                        {
                            foreach (var show in series.Take(10)) // Limit to 10 results
                            {
                                var stats = show.Statistics ?? new Models.SeriesStatistics();
                                results.Add(new Result
                                {
                                    Title = $"Refresh: {show.Title}",
                                    SubTitle = $"Refresh/rescan series: {show.Title} - {show.Status} | {stats.SeasonCount} Seasons",
                                    IcoPath = !string.IsNullOrEmpty(show.PosterPath) ? show.PosterPath : "Images\\icon.png",
                                    Score = 100,
                                    Action = _ =>
                                    {
                                        // Use Task.Run to avoid blocking UI thread
                                        Task.Run(async () =>
                                        {
                                            try
                                            {
                                                await SonarrService.RefreshSeriesAsync(show.Id);
                                                System.Diagnostics.Debug.WriteLine($"Refresh command sent successfully for '{show.Title}'");
                                            }
                                            catch (Exception ex)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"Failed to refresh '{show.Title}': {ex.Message}");
                                            }
                                        });
                                        return true; // Return immediately, don't wait for completion
                                    }
                                });
                            }
                        }
                        else
                        {
                            results.Add(new Result
                            {
                                Title = "No Series Found",
                                SubTitle = $"No series found matching '{searchQuery}'. Try a different search term.",
                                IcoPath = "Images\\icon.png",
                                Score = 100,
                                Action = _ => false
                            });
                        }

                        // Also add option to refresh all
                        results.Add(new Result
                        {
                            Title = "Refresh All Series",
                            SubTitle = "Or refresh all series in Sonarr",
                            IcoPath = "Images\\icon.png",
                            Score = 50,
                            Action = _ =>
                            {
                                // Use Task.Run to avoid blocking UI thread
                                Task.Run(async () =>
                                {
                                    try
                                    {
                                        await SonarrService.RefreshAllSeriesAsync();
                                        System.Diagnostics.Debug.WriteLine("Refresh all series command sent successfully");
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Failed to refresh all series: {ex.Message}");
                                    }
                                });
                                return true; // Return immediately, don't wait for completion
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error searching for series: {ex.Message}");
                        results.Add(new Result
                        {
                            Title = "Search Error",
                            SubTitle = $"Failed to search for series: {ex.Message}",
                            IcoPath = "Images\\icon.png",
                            Score = 100,
                            Action = _ => false
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RefreshCommand: {ex.Message}");
                results.Add(new Result
                {
                    Title = "Error",
                    SubTitle = $"Failed to execute refresh command: {ex.Message}",
                    IcoPath = "Images\\icon.png",
                    Score = 100,
                    Action = _ => false
                });
            }

            return results;
        }
    }
} 