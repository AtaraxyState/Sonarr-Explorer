using Flow.Launcher.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarrFlowLauncherPlugin
{
    public class TestProgram
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Initializing Sonarr Plugin...");
                
                // Set up settings
                var settings = new Settings
                {
                    ApiKey = "411208c5131742b7b5e5c42317f785f7",
                    ServerUrl = "localhost:8989",  // Don't include protocol, it's handled by UseHttps
                    UseHttps = false
                };
                settings.Save();
                
                Console.WriteLine($"Using settings:");
                Console.WriteLine($"  API Key: {settings.ApiKey}");
                Console.WriteLine($"  Server URL: {(settings.UseHttps ? "https://" : "http://")}{settings.ServerUrl}");
                Console.WriteLine($"  HTTPS: {settings.UseHttps}");
                Console.WriteLine();

                var plugin = new Main();
                
                // Initialize plugin context
                var context = new PluginInitContext();
                plugin.Init(context);

                // Test empty query (should show available commands)
                Console.WriteLine("\nTesting empty query...");
                var emptyResults = plugin.Query(new Query("", "", Array.Empty<string>(), Array.Empty<string>(), "sonarr"));
                PrintResults(emptyResults);

                // Test activity view
                Console.WriteLine("\nTesting activity view (-a)...");
                var activityResults = plugin.Query(new Query("-a", "-a", Array.Empty<string>(), Array.Empty<string>(), "sonarr"));
                PrintResults(activityResults);

                // Test library search
                Console.WriteLine("\nTesting library search (-l)...");
                var searchResults = plugin.Query(new Query("-l blue", "-l blue", Array.Empty<string>(), Array.Empty<string>(), "sonarr"));
                PrintResults(searchResults);

                // Test direct search (without flag)
                Console.WriteLine("\nTesting direct search (no flag)...");
                var directResults = plugin.Query(new Query("blue", "blue", Array.Empty<string>(), Array.Empty<string>(), "sonarr"));
                PrintResults(directResults);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void PrintResults(List<Result> results)
        {
            Console.WriteLine($"\nResults found: {results.Count}");
            foreach (var result in results)
            {
                Console.WriteLine("\n----------------------------------------");
                Console.WriteLine($"Title: {result.Title}");
                Console.WriteLine($"Subtitle: {result.SubTitle}");
                Console.WriteLine($"Icon Path: {result.IcoPath}");
                Console.WriteLine($"Score: {result.Score}");
                Console.WriteLine("----------------------------------------");
            }
        }
    }
} 