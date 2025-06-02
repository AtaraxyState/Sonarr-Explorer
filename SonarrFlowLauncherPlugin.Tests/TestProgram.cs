using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Commands;
using SonarrFlowLauncherPlugin.Services;
using System;
using System.IO;

namespace SonarrFlowLauncherPlugin.Tests
{
    public class TestProgram
    {
        private static Settings GetTestSettings()
        {
            // Try to load API key from environment variable first
            string apiKey = Environment.GetEnvironmentVariable("SONARR_API_KEY");
            
            // If not found, try to load from local config file
            if (string.IsNullOrEmpty(apiKey))
            {
                string configPath = Path.Combine("..", "SonarrFlowLauncherPlugin", "plugin.local.yaml");
                if (File.Exists(configPath))
                {
                    string content = File.ReadAllText(configPath);
                    // Simple YAML parsing for ApiKey
                    var lines = content.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Trim().StartsWith("ApiKey:"))
                        {
                            apiKey = line.Split(':')[1].Trim().Trim('"');
                            break;
                        }
                    }
                }
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException(
                    "API Key not found! Please either:\n" +
                    "1. Set SONARR_API_KEY environment variable, or\n" +
                    "2. Create plugin.local.yaml with your API key (copy from plugin.local.yaml.example)");
            }
            
            return new Settings
            {
                ServerUrl = "localhost:8989",
                ApiKey = apiKey,
                UseHttps = false
            };
        }

        private static Settings settings = GetTestSettings();
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