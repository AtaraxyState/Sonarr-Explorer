using System;
using System.Linq;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Services;

namespace SonarrFlowLauncherPlugin.Commands
{
    /// <summary>
    /// Central command router and manager for the Sonarr Flow Launcher plugin.
    /// Handles command parsing, routing, and execution based on user input and API availability.
    /// </summary>
    /// <remarks>
    /// The CommandManager coordinates between different types of commands:
    /// - API-dependent commands (library search, calendar, activity, refresh)
    /// - Offline utility commands (help, about, settings, external links)
    /// - Special command shortcuts for common operations
    /// 
    /// Features include:
    /// - Automatic fallback to offline commands when API is unavailable
    /// - Command flag matching and routing
    /// - Special shortcuts for frequently used operations (-n, -y)
    /// - Help display for available commands based on configuration state
    /// </remarks>
    public class CommandManager
    {
        /// <summary>
        /// Collection of commands that require API connectivity to function
        /// </summary>
        private readonly List<BaseCommand> _commands;
        
        /// <summary>
        /// Collection of commands that work without API connection (utilities, help, setup)
        /// </summary>
        private readonly List<BaseCommand> _offlineCommands;
        
        /// <summary>
        /// Default command used when no specific command flag is provided (library search)
        /// </summary>
        private readonly LibrarySearchCommand _defaultCommand;
        
        /// <summary>
        /// Setup command for guided plugin configuration
        /// </summary>
        private readonly SetupCommand _setupCommand;

        /// <summary>
        /// Initializes a new CommandManager with all available commands and dependencies.
        /// Sets up both API-dependent and offline command collections.
        /// </summary>
        /// <param name="sonarrService">Service for Sonarr API communication</param>
        /// <param name="settings">Plugin settings and configuration</param>
        /// <param name="context">Flow Launcher plugin context (optional, for setup command)</param>
        public CommandManager(SonarrService sonarrService, Settings settings, PluginInitContext? context = null)
        {
            // Setup command (works without API key)
            _setupCommand = new SetupCommand(sonarrService, settings, context);

            // API-dependent commands
            _commands = new List<BaseCommand>
            {
                new ActivityCommand(sonarrService, settings),
                new LibrarySearchCommand(sonarrService, settings),
                new CalendarCommand(sonarrService, settings),
                new RefreshCommand(sonarrService, settings)
            };

            // Offline/utility commands that work without API
            _offlineCommands = new List<BaseCommand>
            {
                _setupCommand,
                new HelpCommand(sonarrService, settings),
                new AboutCommand(sonarrService, settings),
                new UtilityCommand(sonarrService, settings),
                new DateTimeCommand(sonarrService, settings),
                new ExternalLinksCommand(sonarrService, settings)
            };

            _defaultCommand = new LibrarySearchCommand(sonarrService, settings);
        }

        /// <summary>
        /// Processes user queries and routes them to appropriate commands based on flags and API availability.
        /// Implements intelligent command matching, shortcuts, and fallback behavior.
        /// </summary>
        /// <param name="query">User input query containing search terms and command flags</param>
        /// <param name="hasApiKey">Whether API key is configured and API commands are available</param>
        /// <returns>List of results from the matched command or help/command list</returns>
        /// <remarks>
        /// Command routing logic:
        /// 1. Empty query → show available commands list
        /// 2. Special shortcuts (-n, -y) → direct refresh command execution
        /// 3. Offline commands → always available regardless of API status
        /// 4. Alternative command patterns → flexible command matching
        /// 5. API commands → routed if API key is available
        /// 6. Default → library search for unmatched queries
        /// </remarks>
        public List<Result> HandleQuery(Query query, bool hasApiKey = true)
        {
            if (string.IsNullOrEmpty(query.Search))
            {
                return GetAvailableCommands(hasApiKey);
            }

            // Handle special case for standalone -n (overdue refresh) - ONLY if no additional parameters
            if (query.Search.Equals("-n", StringComparison.OrdinalIgnoreCase))
            {
                var refreshCommand = _commands.FirstOrDefault(c => c is RefreshCommand) as RefreshCommand;
                if (refreshCommand != null)
                {
                    return new List<Result>
                    {
                        new Result
                        {
                            Title = "Refresh Overdue Episodes",
                            SubTitle = "Refreshing series with episodes that have already aired today...",
                            IcoPath = "Images\\refresh.png",
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

            // Handle special case for standalone -y (yesterday refresh) - ONLY if no additional parameters
            if (query.Search.Equals("-y", StringComparison.OrdinalIgnoreCase))
            {
                var refreshCommand = _commands.FirstOrDefault(c => c is RefreshCommand) as RefreshCommand;
                if (refreshCommand != null)
                {
                    return new List<Result>
                    {
                        new Result
                        {
                            Title = "Refresh Yesterday's Calendar Series",
                            SubTitle = "Refreshing all series that had episodes in yesterday's calendar...",
                            IcoPath = "Images\\refresh.png",
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

            // Always check offline commands first
            var offlineCommand = _offlineCommands.FirstOrDefault(c => query.Search.StartsWith(c.CommandFlag));
            
            // Check for alternative command patterns
            if (offlineCommand == null)
            {
                if (query.Search.StartsWith("-settings", StringComparison.OrdinalIgnoreCase) ||
                    query.Search.StartsWith("-util", StringComparison.OrdinalIgnoreCase))
                {
                    offlineCommand = _offlineCommands.FirstOrDefault(c => c is UtilityCommand);
                }
                else if (query.Search.StartsWith("-time", StringComparison.OrdinalIgnoreCase))
                {
                    offlineCommand = _offlineCommands.FirstOrDefault(c => c is DateTimeCommand);
                }
                else if (query.Search.StartsWith("-tvdb", StringComparison.OrdinalIgnoreCase) ||
                         query.Search.StartsWith("-imdb", StringComparison.OrdinalIgnoreCase) ||
                         query.Search.StartsWith("-reddit", StringComparison.OrdinalIgnoreCase))
                {
                    offlineCommand = _offlineCommands.FirstOrDefault(c => c is ExternalLinksCommand);
                }
            }
            
            if (offlineCommand != null)
            {
                return offlineCommand.Execute(query);
            }

            // Check API-dependent commands (let them handle their own validation)
            var command = _commands.FirstOrDefault(c => query.Search.StartsWith(c.CommandFlag));
            
            if (command != null)
            {
                return command.Execute(query);
            }

            // Default to library search if no command flag is provided (let it handle its own validation)
            return _defaultCommand.Execute(query);
        }

        /// <summary>
        /// Generates a list of available commands based on current API configuration status.
        /// Provides context-aware help showing only relevant commands.
        /// </summary>
        /// <param name="hasApiKey">Whether API key is configured and API features are available</param>
        /// <returns>List of results showing available commands with descriptions and usage</returns>
        /// <remarks>
        /// Command display logic:
        /// - Always shows offline/utility commands
        /// - Shows API commands only when properly configured
        /// - Includes special shortcuts and usage examples
        /// - Provides configuration guidance when API is unavailable
        /// </remarks>
        private List<Result> GetAvailableCommands(bool hasApiKey)
        {
            var results = new List<Result>();

            // Always show offline commands
            results.AddRange(_offlineCommands.Select(command => new Result
            {
                Title = command.CommandName,
                SubTitle = $"Type {command.CommandFlag} - {command.CommandDescription}",
                IcoPath = "Images\\icon.png",
                Score = 100,
                Action = _ => false
            }));

            // Show API commands if available
            if (hasApiKey)
            {
                results.AddRange(_commands.Select(command => new Result
                {
                    Title = command.CommandName,
                    SubTitle = $"Type {command.CommandFlag} - {command.CommandDescription}",
                    IcoPath = "Images\\icon.png",
                    Score = 90,
                    Action = _ => false
                }));

                // Add additional information for the new calendar refresh shortcuts
                results.Add(new Result
                {
                    Title = "📅 Calendar Refresh Shortcuts",
                    SubTitle = "snr -c (refresh today's calendar) | snr -y (refresh yesterday) | snr -n (refresh overdue episodes)",
                    IcoPath = "Images\\icon.png",
                    Score = 95,
                    Action = _ => false
                });
            }
            else
            {
                results.Add(new Result
                {
                    Title = "⚠️ API Features Disabled",
                    SubTitle = "Configure API key to use calendar, activity, and library search",
                    IcoPath = "Images\\icon.png",
                    Score = 95,
                    Action = _ => false
                });
            }

            return results;
        }

        /// <summary>
        /// Provides offline help information when API features are not available.
        /// Shows commands that work without Sonarr API configuration.
        /// </summary>
        /// <returns>List of results describing offline-capable commands</returns>
        private List<Result> GetOfflineHelp()
        {
            return new List<Result>
            {
                new Result
                {
                    Title = "📋 Available Commands (No API Key)",
                    SubTitle = "These commands work without Sonarr API configuration",
                    IcoPath = "Images\\icon.png",
                    Score = 100,
                    Action = _ => false
                },
                new Result
                {
                    Title = "❓ Help & Commands",
                    SubTitle = "snr -help - Show all available commands",
                    IcoPath = "Images\\icon.png",
                    Score = 95,
                    Action = _ => false
                },
                new Result
                {
                    Title = "ℹ️ Plugin Information",
                    SubTitle = "snr -about - Version, links, and configuration status",
                    IcoPath = "Images\\icon.png",
                    Score = 94,
                    Action = _ => false
                },
                new Result
                {
                    Title = "🔧 Utilities & Testing",
                    SubTitle = "snr -test - Connection testing and settings access",
                    IcoPath = "Images\\icon.png",
                    Score = 93,
                    Action = _ => false
                },
                new Result
                {
                    Title = "🕒 Date & Time Tools",
                    SubTitle = "snr -date - Current time, timezones, and date calculations",
                    IcoPath = "Images\\icon.png",
                    Score = 92,
                    Action = _ => false
                },
                new Result
                {
                    Title = "🔗 External Links",
                    SubTitle = "snr -link - Quick access to TVDB, IMDB, Reddit, documentation",
                    IcoPath = "Images\\icon.png",
                    Score = 91,
                    Action = _ => false
                }
            };
        }
    }
} 
