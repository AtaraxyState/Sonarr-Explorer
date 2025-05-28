using System.Net.Http;
using Newtonsoft.Json;
using SonarrFlowLauncherPlugin.Models;
using System.IO;

namespace SonarrFlowLauncherPlugin.Services
{
    public class SonarrService
    {
        private readonly HttpClient _httpClient;
        private readonly Settings _settings;
        private readonly string _imageCache;

        public SonarrService(Settings settings)
        {
            _settings = settings;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", settings.ApiKey);
            
            // Set up image cache directory
            _imageCache = Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location), "ImageCache");
            if (!Directory.Exists(_imageCache))
            {
                Directory.CreateDirectory(_imageCache);
            }
        }

        private string BaseUrl => $"{(_settings.UseHttps ? "https" : "http")}://{_settings.ServerUrl.TrimEnd('/')}/api/v3";

        public async Task<List<SonarrSeries>> SearchSeriesAsync(string query)
        {
            try
            {
                var url = $"{BaseUrl}/series";
                System.Diagnostics.Debug.WriteLine($"Calling Sonarr API: {url}");
                System.Diagnostics.Debug.WriteLine($"Search query: {query}");

                var response = await _httpClient.GetStringAsync(url);
                System.Diagnostics.Debug.WriteLine($"API Response: {response}");

                var allSeries = JsonConvert.DeserializeObject<List<SonarrSeries>>(response);
                System.Diagnostics.Debug.WriteLine($"Deserialized {allSeries?.Count ?? 0} series");

                // Download posters for each series
                if (allSeries != null)
                {
                    foreach (var series in allSeries)
                    {
                        var poster = series.Images?.FirstOrDefault(i => i.CoverType == "poster");
                        if (poster != null)
                        {
                            var posterUrl = !string.IsNullOrEmpty(poster.RemoteUrl) ? poster.RemoteUrl : poster.Url;
                            if (!string.IsNullOrEmpty(posterUrl))
                            {
                                series.PosterPath = await DownloadPosterAsync(series.Id, posterUrl);
                            }
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(query))
                    return allSeries ?? new List<SonarrSeries>();

                return allSeries?
                    .Where(s => s.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                               (s.Overview?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList() ?? new List<SonarrSeries>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching series: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Let the main class handle the error and show it to the user
            }
        }

        private async Task<string> DownloadPosterAsync(int seriesId, string posterUrl)
        {
            try
            {
                var posterPath = Path.Combine(_imageCache, $"poster_{seriesId}.jpg");
                
                // Check if poster already exists in cache
                if (File.Exists(posterPath))
                {
                    return posterPath;
                }

                // Download the poster
                var imageBytes = await _httpClient.GetByteArrayAsync(posterUrl);
                await File.WriteAllBytesAsync(posterPath, imageBytes);
                
                return posterPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error downloading poster: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> OpenSeriesInBrowser(int seriesId)
        {
            try
            {
                var baseUrl = $"{(_settings.UseHttps ? "https" : "http")}://{_settings.ServerUrl.TrimEnd('/')}";
                var url = $"{baseUrl}/series/{seriesId}";
                
                System.Diagnostics.Debug.WriteLine($"Opening URL: {url}");
                
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening series in browser: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
} 