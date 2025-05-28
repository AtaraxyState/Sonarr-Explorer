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
                new LibrarySearchCommand(sonarrService, settings)
            };

            _defaultCommand = new LibrarySearchCommand(sonarrService, settings);
        }

        public List<Result> HandleQuery(Query query)
        {
            if (string.IsNullOrEmpty(query.Search))
            {
                return GetAvailableCommands();
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
            return _commands.Select(command => new Result
            {
                Title = command.CommandName,
                SubTitle = $"Type {command.CommandFlag} - {command.CommandDescription}",
                IcoPath = "Images\\icon.png",
                Score = 100,
                Action = _ => false
            }).ToList();
        }
    }
} 