using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Commands;
using SonarrFlowLauncherPlugin.Services;

namespace SonarrFlowLauncherPlugin.Tests
{
    public class TestProgram
    {
        private static Settings settings = new Settings
        {
            ServerUrl = "localhost:8989",
            ApiKey = "411208c5131742b7b5e5c42317f785f7",
            UseHttps = false
        };

        private static SonarrService sonarrService = new SonarrService(settings);

        public static void Main(string[] args)
        {
            // Test all commands
            TestCalendarCommand();
            TestActivityCommand();
            TestSearchCommand();
        }

        private static void TestCalendarCommand()
        {
            Console.WriteLine("\nTesting Calendar Command");
            Console.WriteLine("----------------------");

            var command = new CalendarCommand(sonarrService, settings);

            // Test different date ranges
            TestQuery(command, "snr -c today");
            TestQuery(command, "snr -c tomorrow");
            TestQuery(command, "snr -c week");
            TestQuery(command, "snr -c next week");
            TestQuery(command, "snr -c month");
        }

        private static void TestActivityCommand()
        {
            Console.WriteLine("\nTesting Activity Command");
            Console.WriteLine("----------------------");

            var command = new ActivityCommand(sonarrService, settings);
            TestQuery(command, "snr -a");
        }

        private static void TestSearchCommand()
        {
            Console.WriteLine("\nTesting Library Search Command");
            Console.WriteLine("------------------------------");

            var command = new LibrarySearchCommand(sonarrService, settings);
            TestQuery(command, "snr -l");
            TestQuery(command, "snr -l your");
        }

        private static void TestQuery(BaseCommand command, string queryString)
        {
            Console.WriteLine($"\nTesting query: {queryString}");
            var query = new Query(queryString);
            var results = command.Execute(query);

            Console.WriteLine($"Found {results.Count} results:");
            foreach (var result in results)
            {
                Console.WriteLine($"- {result.Title}");
                Console.WriteLine($"  {result.SubTitle}");
                Console.WriteLine($"  Icon: {result.IcoPath}");
                Console.WriteLine();
            }
        }
    }
} 