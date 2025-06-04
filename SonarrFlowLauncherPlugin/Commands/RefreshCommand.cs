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
        public override string CommandDescription => "Refresh all series or search for a specific series to refresh (use: -r [all|c|n|y|{days}|now|series name])";

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
                // Handle specific calendar-based commands
                switch (searchQuery)
                {
                    case "c":
                    case "calendar":
                        return HandleCalendarRefresh();
                    
                    case "n":
                    case "now":
                    case "overdue":
                        return HandleOverdueRefresh();
                    
                    case "y":
                    case "yesterday":
                        return HandleYesterdayRefresh();
                }

                // Check if it's a number (days back)
                if (int.TryParse(searchQuery, out int daysBack) && daysBack > 0)
                {
                    return HandlePriorDaysRefresh(daysBack);
                }

                // If no search query or "all", show refresh all option
                if (string.IsNullOrWhiteSpace(searchQuery) || searchQuery == "all")
                {
                    results.Add(new Result
                    {
                        Title = "Refresh All Series",
                        SubTitle = "Trigger a refresh/rescan of all series in Sonarr",
                        IcoPath = "🔄",
                        Score = 100,
                        Action = _ =>
                        {
                            Task.Run(async () =>
                            {
                                try
                                {
                                    await SonarrService.RefreshAllSeriesAsync();
                                    System.Diagnostics.Debug.WriteLine("All series refresh completed successfully");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Failed to refresh all series: {ex.Message}");
                                }
                            });
                            return true;
                        }
                    });

                    // Add calendar-based options
                    results.Add(new Result
                    {
                        Title = "Refresh Today's Calendar Series",
                        SubTitle = "snr -r c - Refresh all series that have episodes in today's calendar",
                        IcoPath = "🔄",
                        Score = 95,
                        Action = _ =>
                        {
                            Task.Run(async () =>
                            {
                                try
                                {
                                    await SonarrService.RefreshTodaysCalendarSeriesAsync();
                                    System.Diagnostics.Debug.WriteLine("Today's calendar refresh completed successfully");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Failed to refresh today's calendar series: {ex.Message}");
                                }
                            });
                            return true;
                        }
                    });

                    results.Add(new Result
                    {
                        Title = "Refresh Yesterday's Calendar Series",
                        SubTitle = "snr -r y - Refresh all series that had episodes in yesterday's calendar",
                        IcoPath = "🔄",
                        Score = 94,
                        Action = _ =>
                        {
                            Task.Run(async () =>
                            {
                                try
                                {
                                    await SonarrService.RefreshYesterdayCalendarSeriesAsync();
                                    System.Diagnostics.Debug.WriteLine("Yesterday's calendar refresh completed successfully");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Failed to refresh yesterday's calendar series: {ex.Message}");
                                }
                            });
                            return true;
                        }
                    });

                    results.Add(new Result
                    {
                        Title = "Refresh Overdue Episodes",
                        SubTitle = "snr -r n - Refresh series with episodes that have already aired today",
                        IcoPath = "🔄",
                        Score = 93,
                        Action = _ =>
                        {
                            Task.Run(async () =>
                            {
                                try
                                {
                                    await SonarrService.RefreshOverdueCalendarSeriesAsync();
                                    System.Diagnostics.Debug.WriteLine("Overdue refresh completed successfully");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Failed to refresh overdue episodes: {ex.Message}");
                                }
                            });
                            return true;
                        }
                    });

                    results.Add(new Result
                    {
                        Title = "Refresh Prior Days",
                        SubTitle = "snr -r {number} - Refresh series from past N days (e.g., 'snr -r 3' for 3 days back)",
                        IcoPath = "🔄",
                        Score = 92,
                        Action = _ => false
                    });

                    // If no search query, also show options
                    if (string.IsNullOrWhiteSpace(searchQuery))
                    {
                        results.Add(new Result
                        {
                            Title = "Refresh Options",
                            SubTitle = "Use '-r all' (all series), '-r c' (today), '-r y' (yesterday), '-r n' (overdue), '-r {days}' (prior days), or '-r <series name>' (specific series)",
                            IcoPath = "🔄",
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
                                    IcoPath = !string.IsNullOrEmpty(show.PosterPath) ? show.PosterPath : "🔄",
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
                                IcoPath = "🔄",
                                Score = 100,
                                Action = _ => false
                            });
                        }

                        // Also add option to refresh all
                        results.Add(new Result
                        {
                            Title = "Refresh All Series",
                            SubTitle = "Or refresh all series in Sonarr",
                            IcoPath = "🔄",
                            Score = 50,
                            Action = _ =>
                            {
                                // Use Task.Run to avoid blocking UI thread
                                Task.Run(async () =>
                                {
                                    try
                                    {
                                        await SonarrService.RefreshAllSeriesAsync();
                                        System.Diagnostics.Debug.WriteLine("All series refresh completed successfully");
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
                            IcoPath = "🔄",
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
                    IcoPath = "🔄",
                    Score = 100,
                    Action = _ => false
                });
            }

            return results;
        }

        private List<Result> HandleCalendarRefresh()
        {
            return new List<Result>
            {
                new Result
                {
                    Title = "Refresh Today's Calendar Series",
                    SubTitle = "Refreshing all series that have episodes in today's calendar...",
                    IcoPath = "🔄",
                    Score = 100,
                    Action = _ =>
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                var result = await SonarrService.RefreshTodaysCalendarSeriesAsync();
                                System.Diagnostics.Debug.WriteLine($"Calendar refresh result: {result.Message}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to refresh today's calendar series: {ex.Message}");
                            }
                        });
                        return true;
                    }
                }
            };
        }

        private List<Result> HandleOverdueRefresh()
        {
            return new List<Result>
            {
                new Result
                {
                    Title = "Refresh Overdue Episodes",
                    SubTitle = "Refreshing series with episodes that have already aired today...",
                    IcoPath = "🔄",
                    Score = 100,
                    Action = _ =>
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                var result = await SonarrService.RefreshOverdueCalendarSeriesAsync();
                                System.Diagnostics.Debug.WriteLine($"Overdue refresh result: {result.Message}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to refresh overdue episodes: {ex.Message}");
                            }
                        });
                        return true;
                    }
                }
            };
        }

        private List<Result> HandleYesterdayRefresh()
        {
            return new List<Result>
            {
                new Result
                {
                    Title = "Refresh Yesterday's Calendar Series",
                    SubTitle = "Refreshing all series that had episodes in yesterday's calendar...",
                    IcoPath = "🔄",
                    Score = 100,
                    Action = _ =>
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                var result = await SonarrService.RefreshYesterdayCalendarSeriesAsync();
                                System.Diagnostics.Debug.WriteLine($"Yesterday refresh result: {result.Message}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to refresh yesterday's calendar series: {ex.Message}");
                            }
                        });
                        return true;
                    }
                }
            };
        }

        private List<Result> HandlePriorDaysRefresh(int daysBack)
        {
            return new List<Result>
            {
                new Result
                {
                    Title = $"Refresh Prior {daysBack} Day{(daysBack > 1 ? "s" : "")} Calendar Series",
                    SubTitle = $"Refreshing all series that had episodes in the past {daysBack} day{(daysBack > 1 ? "s" : "")}...",
                    IcoPath = "🔄",
                    Score = 100,
                    Action = _ =>
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                var result = await SonarrService.RefreshPriorDaysCalendarSeriesAsync(daysBack);
                                System.Diagnostics.Debug.WriteLine($"Prior days refresh result: {result.Message}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to refresh prior {daysBack} days calendar series: {ex.Message}");
                            }
                        });
                        return true;
                    }
                }
            };
        }
    }
} 