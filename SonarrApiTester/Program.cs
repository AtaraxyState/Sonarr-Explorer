using System;
using System.Threading.Tasks;
using SonarrFlowLauncherPlugin.Services;
using SonarrFlowLauncherPlugin;
using System.Net.Http;

namespace SonarrApiTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Sonarr API Tester - Queue Debug");
            Console.WriteLine("===============================");

            try
            {
                // Make a direct HTTP call to see the raw JSON
                Console.WriteLine("Fetching raw queue data from Sonarr...");
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("X-Api-Key", "411208c5131742b7b5e5c42317f785f7");
                    var queueUrl = "http://localhost:8989/api/v3/queue?pageSize=10&sortKey=timeleft&sortDir=asc&includeEpisode=true&includeSeries=true";
                    
                    Console.WriteLine($"\nCalling: {queueUrl}");
                    var rawResponse = await httpClient.GetStringAsync(queueUrl);
                    
                    Console.WriteLine($"\n=== RAW QUEUE JSON RESPONSE ===");
                    Console.WriteLine(rawResponse);
                    Console.WriteLine("=== END OF RAW RESPONSE ===\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
