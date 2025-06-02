using System;
using System.Threading.Tasks;
using SonarrFlowLauncherPlugin.Services;
using SonarrFlowLauncherPlugin;
using System.Net.Http;
using System.IO;

namespace SonarrApiTester
{
    class Program
    {
        private static string GetApiKey()
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
            
            return apiKey;
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("Sonarr API Tester - Queue Debug");
            Console.WriteLine("===============================");

            try
            {
                string apiKey = GetApiKey();
                Console.WriteLine("API Key loaded successfully (hidden for security)");
                
                // Make a direct HTTP call to see the raw JSON
                Console.WriteLine("Fetching raw queue data from Sonarr...");
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
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
