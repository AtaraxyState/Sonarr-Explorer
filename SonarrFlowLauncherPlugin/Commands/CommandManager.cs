using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Services;

namespace SonarrFlowLauncherPlugin.Commands
{
    public class CommandManager
    {
        private readonly List<BaseCommand> _commands;
        private readonly LibrarySearchCommand _defaultCommand;

        public CommandManager(SonarrService sonarrService, Settings settings)
        {
            _commands = new List<BaseCommand>
            {
                new ActivityCommand(sonarrService, settings),
                new LibrarySearchCommand(sonarrService, settings),
                new CalendarCommand(sonarrService, settings),
                new RefreshCommand(sonarrService, settings)
            };

            _defaultCommand = new LibrarySearchCommand(sonarrService, settings);
        }

        public List<Result> HandleQuery(Query query)
        {
            if (string.IsNullOrEmpty(query.Search))
            {
                return GetAvailableCommands();
            }

            // Handle special case for standalone -c (calendar refresh)
            if (query.Search.StartsWith("-c", StringComparison.OrdinalIgnoreCase))
            {
                var refreshCommand = _commands.FirstOrDefault(c => c is RefreshCommand) as RefreshCommand;
                if (refreshCommand != null)
                {
                    // Direct call to handle calendar refresh
                    return new List<Result>
                    {
                        new Result
                        {
                            Title = "📅 Refresh Today's Calendar Series",
                            SubTitle = "Refreshing all series that have episodes in today's calendar...",
                            IcoPath = "Images\\icon.png",
                            Score = 100,
                            Action = _ =>
                            {
                                Task.Run(async () =>
                                {
                                    try
                                    {
                                        await refreshCommand.GetSonarrService().RefreshTodaysCalendarSeriesAsync();
                                        System.Diagnostics.Debug.WriteLine("Calendar refresh completed successfully");
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
            }

            // Handle special case for standalone -n (overdue refresh)
            if (query.Search.StartsWith("-n", StringComparison.OrdinalIgnoreCase) && 
                !query.Search.StartsWith("-new", StringComparison.OrdinalIgnoreCase)) // Avoid conflict with potential -new commands
            {
                var refreshCommand = _commands.FirstOrDefault(c => c is RefreshCommand) as RefreshCommand;
                if (refreshCommand != null)
                {
                    // Direct call to handle overdue refresh
                    return new List<Result>
                    {
                        new Result
                        {
                            Title = "⏰ Refresh Overdue Episodes",
                            SubTitle = "Refreshing series with episodes that have already aired today...",
                            IcoPath = "Images\\icon.png",
                            Score = 100,
                            Action = _ =>
                            {
                                Task.Run(async () =>
                                {
                                    try
                                    {
                                        await refreshCommand.GetSonarrService().RefreshOverdueCalendarSeriesAsync();
                                        System.Diagnostics.Debug.WriteLine("Overdue refresh completed successfully");
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
            }

            // Handle special case for standalone -y (yesterday refresh)
            if (query.Search.StartsWith("-y", StringComparison.OrdinalIgnoreCase) && 
                !query.Search.StartsWith("-year", StringComparison.OrdinalIgnoreCase)) // Avoid conflict with potential -year commands
            {
                var refreshCommand = _commands.FirstOrDefault(c => c is RefreshCommand) as RefreshCommand;
                if (refreshCommand != null)
                {
                    // Direct call to handle yesterday refresh
                    return new List<Result>
                    {
                        new Result
                        {
                            Title = "📅 Refresh Yesterday's Calendar Series",
                            SubTitle = "Refreshing all series that had episodes in yesterday's calendar...",
                            IcoPath = "Images\\icon.png",
                            Score = 100,
                            Action = _ =>
                            {
                                Task.Run(async () =>
                                {
                                    try
                                    {
                                        await refreshCommand.GetSonarrService().RefreshYesterdayCalendarSeriesAsync();
                                        System.Diagnostics.Debug.WriteLine("Yesterday refresh completed successfully");
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
            }

            var command = _commands.FirstOrDefault(c => query.Search.StartsWith(c.CommandFlag));
            
            if (command != null)
            {
                return command.Execute(query);
            }

            // Default to library search if no command flag is provided
            return _defaultCommand.Execute(query);
        }

        private List<Result> GetAvailableCommands()
        {
            var results = _commands.Select(command => new Result
            {
                Title = command.CommandName,
                SubTitle = $"Type {command.CommandFlag} - {command.CommandDescription}",
                IcoPath = "Images\\icon.png",
                Score = 100,
                Action = _ => false
            }).ToList();

            // Add additional information for the new calendar refresh shortcuts
            results.Add(new Result
            {
                Title = "📅 Calendar Refresh Shortcuts",
                SubTitle = "snr -c (refresh today's calendar) | snr -y (refresh yesterday) | snr -n (refresh overdue episodes)",
                IcoPath = "Images\\icon.png",
                Score = 95,
                Action = _ => false
            });

            return results;
        }
    }
} 