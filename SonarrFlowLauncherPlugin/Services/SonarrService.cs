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

        public async Task<SonarrActivity> GetActivityAsync()
        {
            try
            {
                var activity = new SonarrActivity();

                // Get queue (limit to 10 items)
                var queueUrl = $"{BaseUrl}/queue?pageSize=10&sortKey=timeleft&sortDir=asc";
                var queueResponse = await _httpClient.GetStringAsync(queueUrl);
                var queueData = JsonConvert.DeserializeObject<dynamic>(queueResponse);
                
                if (queueData?.records != null)
                {
                    foreach (var record in queueData.records)
                    {
                        activity.Queue.Add(new SonarrQueueItem
                        {
                            Id = record.id,
                            SeriesId = record.seriesId,
                            Title = record.title ?? string.Empty,
                            SeasonNumber = record.seasonNumber ?? 0,
                            EpisodeNumber = record.episodeNumber ?? 0,
                            Quality = record.quality?.quality?.name ?? string.Empty,
                            Status = record.status ?? string.Empty,
                            Progress = record.progress ?? 0.0,
                            EstimatedCompletionTime = record.estimatedCompletionTime,
                            Protocol = record.protocol ?? string.Empty,
                            DownloadClient = record.downloadClient ?? string.Empty
                        });
                    }
                }

                // Get history (last 10 items)
                var historyUrl = $"{BaseUrl}/history?page=1&pageSize=10&sortKey=date&sortDir=desc";
                var historyResponse = await _httpClient.GetStringAsync(historyUrl);
                var historyData = JsonConvert.DeserializeObject<dynamic>(historyResponse);

                if (historyData?.records != null)
                {
                    foreach (var record in historyData.records)
                    {
                        activity.History.Add(new SonarrHistoryItem
                        {
                            Id = record.id,
                            SeriesId = record.seriesId,
                            Title = record.sourceTitle ?? string.Empty,
                            SeasonNumber = record.episode?.seasonNumber ?? 0,
                            EpisodeNumber = record.episode?.episodeNumber ?? 0,
                            Quality = record.quality?.quality?.name ?? string.Empty,
                            EventType = record.eventType ?? string.Empty,
                            Date = record.date
                        });
                    }
                }

                return activity;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting activity: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> OpenActivityInBrowser()
        {
            try
            {
                var url = $"{(_settings.UseHttps ? "https" : "http")}://{_settings.ServerUrl}/activity";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening activity in browser: {ex.Message}");
                return false;
            }
        }
    }
} 