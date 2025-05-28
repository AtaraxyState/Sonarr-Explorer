using System;
using System.Threading.Tasks;
using SonarrFlowLauncherPlugin.Services;
using SonarrFlowLauncherPlugin;

namespace SonarrApiTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Sonarr API Tester");
            Console.WriteLine("----------------");

            // Create settings
            var settings = new Settings
            {
                ServerUrl = "localhost:8989",
                ApiKey = "411208c5131742b7b5e5c42317f785f7",
                UseHttps = false
            };

            Console.WriteLine($"Testing connection to Sonarr at {settings.ServerUrl}");
            Console.WriteLine("API Key: " + (string.IsNullOrEmpty(settings.ApiKey) ? "Not Set" : "Set"));
            Console.WriteLine();

            var sonarrService = new SonarrService(settings);

            // Test calendar with different date ranges
            await TestCalendarDateRange(sonarrService, "Today", DateTime.Today, DateTime.Today.AddDays(1).AddSeconds(-1));
            await TestCalendarDateRange(sonarrService, "Next 3 Days", DateTime.Today, DateTime.Today.AddDays(3));
            await TestCalendarDateRange(sonarrService, "Next Week", DateTime.Today, DateTime.Today.AddDays(7));
            await TestCalendarDateRange(sonarrService, "Next Month", DateTime.Today, DateTime.Today.AddMonths(1));

            // Test activity
            await TestActivity(sonarrService);

            // Test library search
            await TestLibrarySearch(sonarrService);

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static async Task TestCalendarDateRange(SonarrService sonarrService, string rangeName, DateTime start, DateTime end)
        {
            Console.WriteLine($"\nTesting Calendar Range: {rangeName}");
            Console.WriteLine($"Start: {start:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"End: {end:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("----------------------------------------");

            try
            {
                var calendarItems = await sonarrService.GetCalendarAsync(start, end);
                Console.WriteLine($"Found {calendarItems.Count} episodes");

                foreach (var item in calendarItems)
                {
                    Console.WriteLine($"\n- {item.SeriesTitle}");
                    Console.WriteLine($"  Episode: S{item.SeasonNumber:D2}E{item.EpisodeNumber:D2} - {item.EpisodeTitle}");
                    Console.WriteLine($"  Air Date: {item.AirDate:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"  Has File: {item.HasFile}");
                    Console.WriteLine($"  Monitored: {item.Monitored}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        static async Task TestActivity(SonarrService sonarrService)
        {
            Console.WriteLine("Activity Test");
            Console.WriteLine("-------------");

            var activity = await sonarrService.GetActivityAsync();

            Console.WriteLine("\nQueue Items:");
            Console.WriteLine("------------");
            if (!activity.Queue.Any())
            {
                Console.WriteLine("No items in queue.");
            }
            else
            {
                foreach (var item in activity.Queue)
                {
                    Console.WriteLine($"- {item.Title}");
                    Console.WriteLine($"  Status: {item.Status}");
                    Console.WriteLine($"  Progress: {item.Progress:F1}%");
                    Console.WriteLine($"  Quality: {item.Quality}");
                    if (item.EstimatedCompletionTime.HasValue)
                        Console.WriteLine($"  ETA: {item.EstimatedCompletionTime.Value:g}");
                    Console.WriteLine($"  Protocol: {item.Protocol}");
                    Console.WriteLine($"  Client: {item.DownloadClient}");
                    Console.WriteLine();
                }
            }

            Console.WriteLine("\nHistory Items:");
            Console.WriteLine("--------------");
            if (!activity.History.Any())
            {
                Console.WriteLine("No history items.");
            }
            else
            {
                foreach (var item in activity.History)
                {
                    Console.WriteLine($"- {item.Title}");
                    Console.WriteLine($"  Event: {item.EventType}");
                    Console.WriteLine($"  Date: {item.Date:g}");
                    Console.WriteLine($"  Quality: {item.Quality}");
                    Console.WriteLine($"  Episode: S{item.SeasonNumber:D2}E{item.EpisodeNumber:D2}");
                    Console.WriteLine();
                }
            }
        }

        static async Task TestLibrarySearch(SonarrService sonarrService)
        {
            Console.WriteLine("Library Search Test");
            Console.WriteLine("------------------");
            Console.Write("Enter search term (or press Enter for all series): ");
            var searchTerm = Console.ReadLine();

            var results = await sonarrService.SearchSeriesAsync(searchTerm ?? string.Empty);

            if (!results.Any())
            {
                Console.WriteLine("No series found.");
                return;
            }

            Console.WriteLine($"\nFound {results.Count} series:");
            Console.WriteLine("------------------------");

            foreach (var series in results)
            {
                Console.WriteLine($"\n{series.Title}");
                Console.WriteLine("----------------------------------------");
                Console.WriteLine($"Status: {series.Status}");
                Console.WriteLine($"Network: {series.Network}");
                if (series.Statistics != null)
                {
                    Console.WriteLine($"Episodes: {series.Statistics.EpisodeCount}");
                    Console.WriteLine($"Season Count: {series.Statistics.SeasonCount}");
                    Console.WriteLine($"Size on Disk: {series.Statistics.SizeOnDisk / 1024.0 / 1024.0 / 1024.0:F2} GB");
                }
                Console.WriteLine($"Path: {series.Path}");
                Console.WriteLine($"Monitored: {series.Monitored}");
                if (!string.IsNullOrEmpty(series.Overview))
                {
                    Console.WriteLine("\nOverview:");
                    Console.WriteLine(series.Overview);
                }
                Console.WriteLine();
            }
        }
    }
}
