using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Services;

namespace SonarrFlowLauncherPlugin.Commands
{
    public class CommandManager
    {
        private readonly List<BaseCommand> _commands;
        private readonly List<BaseCommand> _offlineCommands;
        private readonly LibrarySearchCommand _defaultCommand;

        public CommandManager(SonarrService sonarrService, Settings settings)
        {
            // API-dependent commands
            _commands = new List<BaseCommand>
            {
                new ActivityCommand(sonarrService, settings),
                new LibrarySearchCommand(sonarrService, settings),
                new CalendarCommand(sonarrService, settings)
            };

            // Offline/utility commands that work without API
            _offlineCommands = new List<BaseCommand>
            {
                new HelpCommand(sonarrService, settings),
                new AboutCommand(sonarrService, settings),
                new UtilityCommand(sonarrService, settings),
                new DateTimeCommand(sonarrService, settings),
                new ExternalLinksCommand(sonarrService, settings)
            };

            _defaultCommand = new LibrarySearchCommand(sonarrService, settings);
        }

        public List<Result> HandleQuery(Query query, bool hasApiKey = true)
        {
            if (string.IsNullOrEmpty(query.Search))
            {
                return GetAvailableCommands(hasApiKey);
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

            // If we have API key, check API-dependent commands
            if (hasApiKey)
            {
                var command = _commands.FirstOrDefault(c => query.Search.StartsWith(c.CommandFlag));
                
                if (command != null)
                {
                    return command.Execute(query);
                }

                // Default to library search if no command flag is provided
                return _defaultCommand.Execute(query);
            }

            // No API key and no offline command found - show help
            return GetOfflineHelp();
        }

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