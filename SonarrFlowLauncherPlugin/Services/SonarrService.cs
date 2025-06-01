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
                var queueUrl = $"{BaseUrl}/queue?pageSize=10&sortKey=timeleft&sortDir=asc&includeEpisode=true&includeSeries=true";
                var queueResponse = await _httpClient.GetStringAsync(queueUrl);
                
                // Debug: Log the raw response
                System.Diagnostics.Debug.WriteLine($"Queue API Response: {queueResponse}");
                
                var queueData = JsonConvert.DeserializeObject<dynamic>(queueResponse);
                
                if (queueData?.records != null)
                {
                    foreach (var record in queueData.records)
                    {
                        // Debug: Log each record to see the structure
                        System.Diagnostics.Debug.WriteLine($"Queue Record: {JsonConvert.SerializeObject(record)}");
                        
                        var queueItem = new SonarrQueueItem
                        {
                            Id = record.id,
                            SeriesId = record.seriesId,
                            Title = record.series?.title ?? string.Empty,
                            SeasonNumber = record.episode?.seasonNumber ?? 0,
                            EpisodeNumber = record.episode?.episodeNumber ?? 0,
                            Quality = record.quality?.quality?.name ?? string.Empty,
                            Status = record.status ?? string.Empty,
                            Progress = CalculateProgress(record),
                            EstimatedCompletionTime = record.estimatedCompletionTime,
                            Protocol = record.protocol ?? string.Empty,
                            DownloadClient = record.downloadClient ?? string.Empty
                        };
                        
                        // Debug: Log the parsed progress value
                        System.Diagnostics.Debug.WriteLine($"Raw progress value: {record.progress}, Calculated: {queueItem.Progress}");

                        // Get series poster if available
                        if (record.series?.images != null)
                        {
                            foreach (var image in record.series.images)
                            {
                                if ((string)image.coverType == "poster")
                                {
                                    var posterUrl = !string.IsNullOrEmpty((string)image.remoteUrl) ? (string)image.remoteUrl : (string)image.url;
                                    if (!string.IsNullOrEmpty(posterUrl))
                                    {
                                        queueItem.PosterPath = await DownloadPosterAsync(queueItem.SeriesId, posterUrl);
                                    }
                                    break;
                                }
                            }
                        }

                        activity.Queue.Add(queueItem);
                    }
                }

                // Get history (last 10 items)
                var historyUrl = $"{BaseUrl}/history?page=1&pageSize=10&sortKey=date&sortDir=desc&includeSeries=true&includeEpisode=true";
                var historyResponse = await _httpClient.GetStringAsync(historyUrl);
                var historyData = JsonConvert.DeserializeObject<dynamic>(historyResponse);

                if (historyData?.records != null)
                {
                    foreach (var record in historyData.records)
                    {
                        var historyItem = new SonarrHistoryItem
                        {
                            Id = record.id,
                            SeriesId = record.seriesId,
                            Title = record.series?.title ?? record.sourceTitle ?? string.Empty,
                            SeasonNumber = record.episodeInfo?.seasonNumber ?? record.episode?.seasonNumber ?? 0,
                            EpisodeNumber = record.episodeInfo?.episodeNumber ?? record.episode?.episodeNumber ?? 0,
                            Quality = record.quality?.quality?.name ?? string.Empty,
                            EventType = record.eventType ?? string.Empty,
                            Date = record.date
                        };

                        // Get series poster if available
                        if (record.series?.images != null)
                        {
                            foreach (var image in record.series.images)
                            {
                                if ((string)image.coverType == "poster")
                                {
                                    var posterUrl = !string.IsNullOrEmpty((string)image.remoteUrl) ? (string)image.remoteUrl : (string)image.url;
                                    if (!string.IsNullOrEmpty(posterUrl))
                                    {
                                        historyItem.PosterPath = await DownloadPosterAsync(historyItem.SeriesId, posterUrl);
                                    }
                                    break;
                                }
                            }
                        }

                        activity.History.Add(historyItem);
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

        public async Task<List<SonarrCalendarItem>> GetCalendarAsync(DateTime? start = null, DateTime? end = null)
        {
            try
            {
                start ??= DateTime.Today;
                end ??= DateTime.Today.AddDays(7); // Default to a week from today

                var url = $"{BaseUrl}/calendar?start={start:yyyy-MM-dd HH:mm:ss}&end={end:yyyy-MM-dd HH:mm:ss}&includeSeries=true";
                System.Diagnostics.Debug.WriteLine($"Calling Sonarr Calendar API: {url}");
                
                var response = await _httpClient.GetStringAsync(url);
                System.Diagnostics.Debug.WriteLine($"Calendar API Response: {response}");
                
                var calendarData = JsonConvert.DeserializeObject<List<dynamic>>(response);
                System.Diagnostics.Debug.WriteLine($"Deserialized {calendarData?.Count ?? 0} calendar items");

                var calendarItems = new List<SonarrCalendarItem>();

                if (calendarData != null)
                {
                    foreach (var item in calendarData)
                    {
                        try
                        {
                            var calendarItem = new SonarrCalendarItem
                            {
                                Id = item.id,
                                SeriesId = item.seriesId,
                                Title = item.series?.title ?? string.Empty,
                                SeriesTitle = item.series?.title ?? string.Empty,
                                EpisodeTitle = item.title ?? string.Empty,
                                SeasonNumber = item.seasonNumber ?? 0,
                                EpisodeNumber = item.episodeNumber ?? 0,
                                AirDate = item.airDate ?? DateTime.MinValue,
                                HasFile = item.hasFile ?? false,
                                Monitored = item.monitored ?? false,
                                Overview = item.overview ?? string.Empty
                            };

                            // Get series poster if available
                            if (item.series?.images != null)
                            {
                                foreach (var image in item.series.images)
                                {
                                    if ((string)image.coverType == "poster")
                                    {
                                        var posterUrl = !string.IsNullOrEmpty((string)image.remoteUrl) ? (string)image.remoteUrl : (string)image.url;
                                        if (!string.IsNullOrEmpty(posterUrl))
                                        {
                                            calendarItem.PosterPath = await DownloadPosterAsync(calendarItem.SeriesId, posterUrl);
                                        }
                                        break;
                                    }
                                }
                            }

                            calendarItems.Add(calendarItem);
                            System.Diagnostics.Debug.WriteLine($"Added calendar item: {calendarItem.Title} S{calendarItem.SeasonNumber:D2}E{calendarItem.EpisodeNumber:D2} - {calendarItem.EpisodeTitle}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing calendar item: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"Raw item data: {JsonConvert.SerializeObject(item)}");
                        }
                    }
                }

                return calendarItems;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting calendar: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> OpenCalendarInBrowser()
        {
            try
            {
                var url = $"{(_settings.UseHttps ? "https" : "http")}://{_settings.ServerUrl}/calendar";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening calendar in browser: {ex.Message}");
                return false;
            }
        }

        private double CalculateProgress(dynamic record)
        {
            try
            {
                // Try to calculate from sizeleft and size first (Sonarr uses 'size' for total, not 'sizetotal')
                if (record.sizeleft != null && record.size != null)
                {
                    long sizeLeft = (long)record.sizeleft;
                    long sizeTotal = (long)record.size;
                    
                    if (sizeTotal > 0)
                    {
                        // Progress = (total - left) / total * 100
                        double progress = ((double)(sizeTotal - sizeLeft) / sizeTotal) * 100.0;
                        System.Diagnostics.Debug.WriteLine($"Calculated progress from size: {sizeLeft}/{sizeTotal} = {progress:F1}%");
                        return progress;
                    }
                }
                
                // Fallback to direct progress field
                if (record.progress != null)
                {
                    double progress = (double)record.progress;
                    System.Diagnostics.Debug.WriteLine($"Using direct progress field: {progress}");
                    return progress;
                }
                
                System.Diagnostics.Debug.WriteLine($"No progress information available in record");
                return 0.0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating progress: {ex.Message}");
                return 0.0;
            }
        }
    }
} 