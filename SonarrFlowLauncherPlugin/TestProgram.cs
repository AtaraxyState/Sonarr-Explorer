using Flow.Launcher.Plugin;
using System;
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

                // Test a search query
                Console.WriteLine("\nTesting search query...");
                var query = new Query("test", "test", Array.Empty<string>(), Array.Empty<string>(), "sonarr");
                var results = plugin.Query(query);

                Console.WriteLine($"\nResults found: {results.Count}");
                foreach (var result in results)
                {
                    Console.WriteLine("\n----------------------------------------");
                    Console.WriteLine($"Title: {result.Title}");
                    Console.WriteLine($"Subtitle: {result.SubTitle}");
                    Console.WriteLine($"Icon Path: {result.IcoPath}");
                    
                    // Try to get the underlying show object through reflection
                    var showField = result.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .FirstOrDefault(f => f.FieldType == typeof(Models.SonarrSeries));
                    
                    if (showField != null)
                    {
                        var show = showField.GetValue(result) as Models.SonarrSeries;
                        if (show != null)
                        {
                            Console.WriteLine("\nDetailed Information:");
                            Console.WriteLine($"  ID: {show.Id}");
                            Console.WriteLine($"  Network: {show.Network}");
                            Console.WriteLine($"  Status: {show.Status}");
                            Console.WriteLine($"  Path: {show.Path}");
                            
                            if (show.Statistics != null)
                            {
                                Console.WriteLine("\nStatistics:");
                                Console.WriteLine($"  Season Count: {show.Statistics.SeasonCount}");
                                Console.WriteLine($"  Episode Count: {show.Statistics.EpisodeCount}");
                                Console.WriteLine($"  Episode File Count: {show.Statistics.EpisodeFileCount}");
                                Console.WriteLine($"  Total Episode Count: {show.Statistics.TotalEpisodeCount}");
                                Console.WriteLine($"  Size on Disk: {show.Statistics.SizeOnDisk / 1024 / 1024} MB");
                            }

                            if (show.Images != null && show.Images.Any())
                            {
                                Console.WriteLine("\nImages:");
                                foreach (var image in show.Images)
                                {
                                    Console.WriteLine($"  Type: {image.CoverType}");
                                    Console.WriteLine($"  URL: {image.Url}");
                                    Console.WriteLine($"  Remote URL: {image.RemoteUrl}");
                                }
                            }

                            if (!string.IsNullOrEmpty(show.PosterPath))
                            {
                                Console.WriteLine($"\nDownloaded Poster: {show.PosterPath}");
                            }
                        }
                    }
                    Console.WriteLine("----------------------------------------");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
} 