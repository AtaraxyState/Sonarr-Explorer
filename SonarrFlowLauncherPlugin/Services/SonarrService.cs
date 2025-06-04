using System.Net.Http;
using Newtonsoft.Json;
using SonarrFlowLauncherPlugin.Models;
using System.IO;

namespace SonarrFlowLauncherPlugin.Services
{
    public class SonarrService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Settings _settings;
        private readonly string _imageCache;
        private bool _disposed;

        // Constants
        private const string PosterCoverType = "poster";
        private const string ImageCacheDirName = "ImageCache";

        public SonarrService(Settings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _httpClient = CreateHttpClient(settings);
            _imageCache = InitializeImageCache();
        }

        private HttpClient CreateHttpClient(Settings settings)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", settings.ApiKey);
            return client;
        }

        private string InitializeImageCache()
        {
            var cacheDir = Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location), ImageCacheDirName);
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }
            return cacheDir;
        }

        private string BaseUrl => $"{(_settings.UseHttps ? "https" : "http")}://{_settings.ServerUrl.TrimEnd('/')}/api/v3";
        private string WebBaseUrl => $"{(_settings.UseHttps ? "https" : "http")}://{_settings.ServerUrl.TrimEnd('/')}";

        #region Series Operations

        public async Task<List<SonarrSeries>> SearchSeriesAsync(string query)
        {
            try
            {
                var url = $"{BaseUrl}/series";
                LogDebug($"Fetching series from: {url}");

                var response = await _httpClient.GetStringAsync(url);
                var allSeries = JsonConvert.DeserializeObject<List<SonarrSeries>>(response) ?? new List<SonarrSeries>();
                
                LogDebug($"Retrieved {allSeries.Count} series");

                // Download posters for each series
                await DownloadPostersForSeriesAsync(allSeries);

                // Filter by query if provided
                return FilterSeries(allSeries, query);
            }
            catch (Exception ex)
            {
                LogError("Error searching series", ex);
                throw;
            }
        }

        private async Task DownloadPostersForSeriesAsync(IEnumerable<SonarrSeries> series)
        {
            foreach (var seriesItem in series)
            {
                var posterUrl = ExtractPosterUrl(seriesItem.Images);
                if (!string.IsNullOrEmpty(posterUrl))
                {
                    seriesItem.PosterPath = await DownloadPosterAsync(seriesItem.Id, posterUrl);
                }
            }
        }

        private List<SonarrSeries> FilterSeries(List<SonarrSeries> allSeries, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return allSeries;

            return allSeries
                .Where(s => s.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                           (s.Overview?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        public async Task<bool> OpenSeriesInBrowser(string titleSlug)
        {
            try
            {
                var url = $"{WebBaseUrl}/series/{titleSlug}";
                return OpenUrlInBrowser(url);
            }
            catch (Exception ex)
            {
                LogError($"Error opening series {titleSlug} in browser", ex);
                return false;
            }
        }

        #endregion

        #region Activity Operations

        public async Task<SonarrActivity> GetActivityAsync()
        {
            try
            {
                var activity = new SonarrActivity();

                // Fetch queue and history in parallel
                var queueTask = FetchQueueItemsAsync();
                var historyTask = FetchHistoryItemsAsync();

                await Task.WhenAll(queueTask, historyTask);

                activity.Queue = await queueTask;
                activity.History = await historyTask;

                return activity;
            }
            catch (Exception ex)
            {
                LogError("Error getting activity", ex);
                throw;
            }
        }

        private async Task<List<SonarrQueueItem>> FetchQueueItemsAsync()
        {
            var queueUrl = $"{BaseUrl}/queue?sortKey=timeleft&sortDir=asc&includeEpisode=true&includeSeries=true";
            var response = await _httpClient.GetStringAsync(queueUrl);
            
            LogDebug("Queue API Response received");
            
            var queueData = JsonConvert.DeserializeObject<dynamic>(response);
            var queueItems = new List<SonarrQueueItem>();

            if (queueData?.records != null)
            {
                foreach (var record in queueData.records)
                {
                    var queueItem = await ParseQueueRecordAsync(record);
                    queueItems.Add(queueItem);
                }
            }

            return queueItems;
        }

        private async Task<SonarrQueueItem> ParseQueueRecordAsync(dynamic record)
        {
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

            // Download poster if available
            var posterUrl = ExtractPosterUrlFromRecord(record.series?.images);
            if (!string.IsNullOrEmpty(posterUrl))
            {
                queueItem.PosterPath = await DownloadPosterAsync(queueItem.SeriesId, posterUrl);
            }

            return queueItem;
        }

        private async Task<List<SonarrHistoryItem>> FetchHistoryItemsAsync()
        {
            var historyUrl = $"{BaseUrl}/history?sortKey=date&sortDir=desc&includeSeries=true&includeEpisode=true";
            var response = await _httpClient.GetStringAsync(historyUrl);
            var historyData = JsonConvert.DeserializeObject<dynamic>(response);
            var historyItems = new List<SonarrHistoryItem>();

            if (historyData?.records != null)
            {
                foreach (var record in historyData.records)
                {
                    var historyItem = await ParseHistoryRecordAsync(record);
                    historyItems.Add(historyItem);
                }
            }

            return historyItems;
        }

        private async Task<SonarrHistoryItem> ParseHistoryRecordAsync(dynamic record)
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

            // Download poster if available
            var posterUrl = ExtractPosterUrlFromRecord(record.series?.images);
            if (!string.IsNullOrEmpty(posterUrl))
            {
                historyItem.PosterPath = await DownloadPosterAsync(historyItem.SeriesId, posterUrl);
            }

            return historyItem;
        }

        public async Task<bool> OpenActivityInBrowser()
        {
            try
            {
                var url = $"{WebBaseUrl}/activity";
                return OpenUrlInBrowser(url);
            }
            catch (Exception ex)
            {
                LogError("Error opening activity in browser", ex);
                return false;
            }
        }

        #endregion

        #region Calendar Operations

        public async Task<List<SonarrCalendarItem>> GetCalendarAsync(DateTime? start = null, DateTime? end = null)
        {
            try
            {
                start ??= DateTime.Today;
                end ??= DateTime.Today.AddDays(7);

                var url = $"{BaseUrl}/calendar?start={start:yyyy-MM-dd HH:mm:ss}&end={end:yyyy-MM-dd HH:mm:ss}&includeSeries=true";
                LogDebug($"Fetching calendar from: {url}");
                
                var response = await _httpClient.GetStringAsync(url);
                var calendarData = JsonConvert.DeserializeObject<List<dynamic>>(response);
                
                LogDebug($"Retrieved {calendarData?.Count ?? 0} calendar items");

                var calendarItems = new List<SonarrCalendarItem>();

                if (calendarData != null)
                {
                    foreach (var item in calendarData)
                    {
                        try
                        {
                            var calendarItem = await ParseCalendarItemAsync(item);
                            calendarItems.Add(calendarItem);
                        }
                        catch (Exception ex)
                        {
                            LogError($"Error processing calendar item: {JsonConvert.SerializeObject(item)}", ex);
                        }
                    }
                }

                return calendarItems;
            }
            catch (Exception ex)
            {
                LogError("Error getting calendar", ex);
                throw;
            }
        }

        private async Task<SonarrCalendarItem> ParseCalendarItemAsync(dynamic item)
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
                AirDate = ParseAirDate(item.airDate),
                HasFile = item.hasFile ?? false,
                Monitored = item.monitored ?? false,
                Overview = item.overview ?? string.Empty
            };

            // Download poster if available
            var posterUrl = ExtractPosterUrlFromRecord(item.series?.images);
            if (!string.IsNullOrEmpty(posterUrl))
            {
                calendarItem.PosterPath = await DownloadPosterAsync(calendarItem.SeriesId, posterUrl);
            }

            LogDebug($"Processed calendar item: {calendarItem.Title} S{calendarItem.SeasonNumber:D2}E{calendarItem.EpisodeNumber:D2} - Air Date: {calendarItem.AirDate:yyyy-MM-dd HH:mm:ss} ({calendarItem.AirDate.Kind})");
            return calendarItem;
        }

        private DateTime ParseAirDate(dynamic airDateValue)
        {
            try
            {
                if (airDateValue == null)
                {
                    LogDebug("Air date is null, using DateTime.MinValue");
                    return DateTime.MinValue;
                }

                // Convert the dynamic value to string first
                string airDateString = airDateValue.ToString();
                LogDebug($"Raw air date from API: {airDateString}");

                // Try to parse the datetime string
                if (DateTime.TryParse(airDateString, out DateTime parsedDate))
                {
                    LogDebug($"Parsed air date: {parsedDate:yyyy-MM-dd HH:mm:ss} (Kind: {parsedDate.Kind})");
                    
                    // Sonarr typically returns UTC times, so ensure they're marked as UTC
                    if (parsedDate.Kind == DateTimeKind.Unspecified)
                    {
                        // Assume UTC if not specified (common for Sonarr API)
                        parsedDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                        LogDebug($"Specified as UTC: {parsedDate:yyyy-MM-dd HH:mm:ss} ({parsedDate.Kind})");
                    }
                    
                    return parsedDate;
                }
                else
                {
                    LogError($"Failed to parse air date string: {airDateString}", new Exception("DateTime parsing failed"));
                    return DateTime.MinValue;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error parsing air date: {airDateValue}", ex);
                return DateTime.MinValue;
            }
        }

        public async Task<bool> OpenCalendarInBrowser()
        {
            try
            {
                var url = $"{WebBaseUrl}/calendar";
                return OpenUrlInBrowser(url);
            }
            catch (Exception ex)
            {
                LogError("Error opening calendar in browser", ex);
                return false;
            }
        }

        #endregion

        #region Helper Methods

        private string ExtractPosterUrl(IEnumerable<SonarrImage> images)
        {
            var poster = images?.FirstOrDefault(i => i.CoverType == PosterCoverType);
            return poster != null ? 
                (!string.IsNullOrEmpty(poster.RemoteUrl) ? poster.RemoteUrl : poster.Url) : 
                null;
        }

        private string ExtractPosterUrlFromRecord(dynamic images)
        {
            if (images == null) return null;

            foreach (var image in images)
            {
                if ((string)image.coverType == PosterCoverType)
                {
                    return !string.IsNullOrEmpty((string)image.remoteUrl) ? 
                        (string)image.remoteUrl : (string)image.url;
                }
            }
            return null;
        }

        private async Task<string> DownloadPosterAsync(int seriesId, string posterUrl)
        {
            try
            {
                var posterPath = Path.Combine(_imageCache, $"poster_{seriesId}.jpg");
                
                // Check cache first
                if (File.Exists(posterPath))
                    return posterPath;

                // Download and cache
                var imageBytes = await _httpClient.GetByteArrayAsync(posterUrl);
                await File.WriteAllBytesAsync(posterPath, imageBytes);
                
                return posterPath;
            }
            catch (Exception ex)
            {
                LogError($"Error downloading poster for series {seriesId}", ex);
                return null;
            }
        }

        private double CalculateProgress(dynamic record)
        {
            try
            {
                // Try to calculate from size fields (Sonarr uses 'size' for total)
                if (record.sizeleft != null && record.size != null)
                {
                    long sizeLeft = (long)record.sizeleft;
                    long sizeTotal = (long)record.size;
                    
                    if (sizeTotal > 0)
                    {
                        double progress = ((double)(sizeTotal - sizeLeft) / sizeTotal) * 100.0;
                        LogDebug($"Calculated progress: {sizeLeft}/{sizeTotal} = {progress:F1}%");
                        return progress;
                    }
                }
                
                // Fallback to direct progress field
                if (record.progress != null)
                {
                    double progress = (double)record.progress;
                    LogDebug($"Using direct progress: {progress}%");
                    return progress;
                }
                
                return 0.0;
            }
            catch (Exception ex)
            {
                LogError("Error calculating progress", ex);
                return 0.0;
            }
        }

        private bool OpenUrlInBrowser(string url)
        {
            try
            {
                LogDebug($"Opening URL: {url}");
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
                LogError($"Error opening URL: {url}", ex);
                return false;
            }
        }

        #endregion

        #region Refresh Operations

        public async Task<bool> RefreshAllSeriesAsync()
        {
            try
            {
                var commandUrl = $"{BaseUrl}/command";
                var command = new { name = "RescanSeries" };
                var json = JsonConvert.SerializeObject(command);

                LogDebug($"Sending refresh all series command to: {commandUrl}");
                LogDebug($"Command payload: {json}");

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(commandUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    LogDebug("Refresh all series command sent successfully");
                    return true;
                }
                else
                {
                    LogError($"Failed to send refresh command. Status: {response.StatusCode}", new Exception(response.ReasonPhrase));
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError("Error refreshing all series", ex);
                return false;
            }
        }

        public async Task<bool> RefreshSeriesAsync(int seriesId)
        {
            try
            {
                var commandUrl = $"{BaseUrl}/command";
                var command = new { name = "RescanSeries", seriesId = seriesId };
                var json = JsonConvert.SerializeObject(command);

                LogDebug($"Sending refresh series command to: {commandUrl}");
                LogDebug($"Command payload: {json}");

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(commandUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    LogDebug($"Refresh series {seriesId} command sent successfully");
                    return true;
                }
                else
                {
                    LogError($"Failed to send refresh command for series {seriesId}. Status: {response.StatusCode}", new Exception(response.ReasonPhrase));
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error refreshing series {seriesId}", ex);
                return false;
            }
        }

        public async Task<RefreshCalendarResult> RefreshTodaysCalendarSeriesAsync()
        {
            try
            {
                LogDebug("Starting refresh of today's calendar series");
                
                // Get today's calendar entries
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var todaysEpisodes = await GetCalendarAsync(today, tomorrow);

                if (!todaysEpisodes.Any())
                {
                    LogDebug("No episodes found in today's calendar");
                    return new RefreshCalendarResult 
                    { 
                        Success = true, 
                        SeriesRefreshed = 0, 
                        Message = "No episodes found in today's calendar" 
                    };
                }

                // Get unique series IDs from today's episodes
                var uniqueSeriesIds = todaysEpisodes.Select(e => e.SeriesId).Distinct().ToList();
                LogDebug($"Found {uniqueSeriesIds.Count} unique series in today's calendar");

                int successCount = 0;
                var failedSeries = new List<string>();

                // Refresh each series
                foreach (var seriesId in uniqueSeriesIds)
                {
                    try
                    {
                        var success = await RefreshSeriesAsync(seriesId);
                        if (success)
                        {
                            successCount++;
                            var seriesTitle = todaysEpisodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                            LogDebug($"Successfully refreshed series: {seriesTitle}");
                        }
                        else
                        {
                            var seriesTitle = todaysEpisodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                            failedSeries.Add(seriesTitle);
                            LogError($"Failed to refresh series: {seriesTitle}", new Exception("Refresh command failed"));
                        }

                        // Small delay between requests to avoid overwhelming the API
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        var seriesTitle = todaysEpisodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                        failedSeries.Add(seriesTitle);
                        LogError($"Exception refreshing series: {seriesTitle}", ex);
                    }
                }

                var message = $"Refreshed {successCount} of {uniqueSeriesIds.Count} series from today's calendar";
                if (failedSeries.Any())
                {
                    message += $". Failed: {string.Join(", ", failedSeries)}";
                }

                LogDebug(message);
                return new RefreshCalendarResult 
                { 
                    Success = true, 
                    SeriesRefreshed = successCount, 
                    TotalSeries = uniqueSeriesIds.Count,
                    Message = message,
                    FailedSeries = failedSeries
                };
            }
            catch (Exception ex)
            {
                LogError("Error refreshing today's calendar series", ex);
                return new RefreshCalendarResult 
                { 
                    Success = false, 
                    SeriesRefreshed = 0, 
                    Message = $"Error: {ex.Message}" 
                };
            }
        }

        public async Task<RefreshCalendarResult> RefreshOverdueCalendarSeriesAsync()
        {
            try
            {
                LogDebug("Starting refresh of overdue calendar series");
                
                // Get today's calendar entries
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var todaysEpisodes = await GetCalendarAsync(today, tomorrow);

                if (!todaysEpisodes.Any())
                {
                    LogDebug("No episodes found in today's calendar");
                    return new RefreshCalendarResult 
                    { 
                        Success = true, 
                        SeriesRefreshed = 0, 
                        Message = "No episodes found in today's calendar" 
                    };
                }

                var now = DateTime.Now;
                LogDebug($"Current local time: {now:yyyy-MM-dd HH:mm:ss}");
                LogDebug($"Found {todaysEpisodes.Count} episodes in today's calendar");

                // Filter episodes that have already aired (past their air date/time)
                var overdueEpisodes = new List<SonarrCalendarItem>();
                
                foreach (var episode in todaysEpisodes)
                {
                    LogDebug($"Checking episode: {episode.SeriesTitle} S{episode.SeasonNumber:D2}E{episode.EpisodeNumber:D2} - Air Date: {episode.AirDate:yyyy-MM-dd HH:mm:ss} ({episode.AirDate.Kind})");
                    
                    // Handle timezone conversion if needed
                    var episodeAirTime = episode.AirDate;
                    
                    // If the episode air date is in UTC, convert to local time for comparison
                    if (episodeAirTime.Kind == DateTimeKind.Utc)
                    {
                        episodeAirTime = episodeAirTime.ToLocalTime();
                        LogDebug($"  Converted UTC to local time: {episodeAirTime:yyyy-MM-dd HH:mm:ss}");
                    }
                    else if (episodeAirTime.Kind == DateTimeKind.Unspecified)
                    {
                        // If kind is unspecified, assume it's already in the correct timezone
                        // but we might need to handle this based on Sonarr's settings
                        LogDebug($"  Air time kind is unspecified, treating as local time");
                    }

                    // Add some buffer time (e.g., 10 minutes) to account for slight variations
                    var bufferMinutes = 10;
                    var cutoffTime = now.AddMinutes(-bufferMinutes);
                    
                    if (episodeAirTime <= cutoffTime)
                    {
                        var minutesLate = (now - episodeAirTime).TotalMinutes;
                        LogDebug($"  Episode is overdue by {minutesLate:F1} minutes");
                        overdueEpisodes.Add(episode);
                    }
                    else
                    {
                        var minutesUntilAir = (episodeAirTime - now).TotalMinutes;
                        LogDebug($"  Episode airs in {minutesUntilAir:F1} minutes");
                    }
                }

                if (!overdueEpisodes.Any())
                {
                    LogDebug("No overdue episodes found in today's calendar (checked with 10-minute buffer)");
                    return new RefreshCalendarResult 
                    { 
                        Success = true, 
                        SeriesRefreshed = 0, 
                        Message = "No overdue episodes found in today's calendar" 
                    };
                }

                // Get unique series IDs from overdue episodes
                var uniqueSeriesIds = overdueEpisodes.Select(e => e.SeriesId).Distinct().ToList();
                LogDebug($"Found {uniqueSeriesIds.Count} unique series with {overdueEpisodes.Count} overdue episodes");

                // Log which episodes are considered overdue
                foreach (var episode in overdueEpisodes)
                {
                    var minutesLate = (now - episode.AirDate).TotalMinutes;
                    LogDebug($"Overdue: {episode.SeriesTitle} S{episode.SeasonNumber:D2}E{episode.EpisodeNumber:D2} - {minutesLate:F1} minutes late");
                }

                int successCount = 0;
                var failedSeries = new List<string>();

                // Refresh each series with overdue episodes
                foreach (var seriesId in uniqueSeriesIds)
                {
                    try
                    {
                        var success = await RefreshSeriesAsync(seriesId);
                        if (success)
                        {
                            successCount++;
                            var seriesTitle = overdueEpisodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                            LogDebug($"Successfully refreshed overdue series: {seriesTitle}");
                        }
                        else
                        {
                            var seriesTitle = overdueEpisodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                            failedSeries.Add(seriesTitle);
                            LogError($"Failed to refresh overdue series: {seriesTitle}", new Exception("Refresh command failed"));
                        }

                        // Small delay between requests to avoid overwhelming the API
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        var seriesTitle = overdueEpisodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                        failedSeries.Add(seriesTitle);
                        LogError($"Exception refreshing overdue series: {seriesTitle}", ex);
                    }
                }

                var message = $"Refreshed {successCount} of {uniqueSeriesIds.Count} series with {overdueEpisodes.Count} overdue episodes from today's calendar";
                if (failedSeries.Any())
                {
                    message += $". Failed: {string.Join(", ", failedSeries)}";
                }

                LogDebug(message);
                return new RefreshCalendarResult 
                { 
                    Success = true, 
                    SeriesRefreshed = successCount, 
                    TotalSeries = uniqueSeriesIds.Count,
                    Message = message,
                    FailedSeries = failedSeries
                };
            }
            catch (Exception ex)
            {
                LogError("Error refreshing overdue calendar series", ex);
                return new RefreshCalendarResult 
                { 
                    Success = false, 
                    SeriesRefreshed = 0, 
                    Message = $"Error: {ex.Message}" 
                };
            }
        }

        public async Task<RefreshCalendarResult> RefreshYesterdayCalendarSeriesAsync()
        {
            try
            {
                LogDebug("Starting refresh of yesterday's calendar series");
                
                // Get yesterday's calendar entries
                var yesterday = DateTime.Today.AddDays(-1);
                var today = DateTime.Today;
                var yesterdaysEpisodes = await GetCalendarAsync(yesterday, today);

                if (!yesterdaysEpisodes.Any())
                {
                    LogDebug("No episodes found in yesterday's calendar");
                    return new RefreshCalendarResult 
                    { 
                        Success = true, 
                        SeriesRefreshed = 0, 
                        Message = "No episodes found in yesterday's calendar" 
                    };
                }

                // Get unique series IDs from yesterday's episodes
                var uniqueSeriesIds = yesterdaysEpisodes.Select(e => e.SeriesId).Distinct().ToList();
                LogDebug($"Found {uniqueSeriesIds.Count} unique series in yesterday's calendar");

                int successCount = 0;
                var failedSeries = new List<string>();

                // Refresh each series
                foreach (var seriesId in uniqueSeriesIds)
                {
                    try
                    {
                        var success = await RefreshSeriesAsync(seriesId);
                        if (success)
                        {
                            successCount++;
                            var seriesTitle = yesterdaysEpisodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                            LogDebug($"Successfully refreshed series from yesterday: {seriesTitle}");
                        }
                        else
                        {
                            var seriesTitle = yesterdaysEpisodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                            failedSeries.Add(seriesTitle);
                            LogError($"Failed to refresh series from yesterday: {seriesTitle}", new Exception("Refresh command failed"));
                        }

                        // Small delay between requests to avoid overwhelming the API
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        var seriesTitle = yesterdaysEpisodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                        failedSeries.Add(seriesTitle);
                        LogError($"Exception refreshing series from yesterday: {seriesTitle}", ex);
                    }
                }

                var message = $"Refreshed {successCount} of {uniqueSeriesIds.Count} series from yesterday's calendar";
                if (failedSeries.Any())
                {
                    message += $". Failed: {string.Join(", ", failedSeries)}";
                }

                LogDebug(message);
                return new RefreshCalendarResult 
                { 
                    Success = true, 
                    SeriesRefreshed = successCount, 
                    TotalSeries = uniqueSeriesIds.Count,
                    Message = message,
                    FailedSeries = failedSeries
                };
            }
            catch (Exception ex)
            {
                LogError("Error refreshing yesterday's calendar series", ex);
                return new RefreshCalendarResult 
                { 
                    Success = false, 
                    SeriesRefreshed = 0, 
                    Message = $"Error: {ex.Message}" 
                };
            }
        }

        public async Task<RefreshCalendarResult> RefreshPriorDaysCalendarSeriesAsync(int daysBack)
        {
            try
            {
                LogDebug($"Starting refresh of calendar series from {daysBack} days back");
                
                if (daysBack < 1)
                {
                    return new RefreshCalendarResult 
                    { 
                        Success = false, 
                        SeriesRefreshed = 0, 
                        Message = "Days back must be 1 or greater" 
                    };
                }

                // Get calendar entries from specified days back
                var startDate = DateTime.Today.AddDays(-daysBack);
                var endDate = DateTime.Today;
                var priorEpisodes = await GetCalendarAsync(startDate, endDate);

                if (!priorEpisodes.Any())
                {
                    LogDebug($"No episodes found in calendar from {daysBack} days back");
                    return new RefreshCalendarResult 
                    { 
                        Success = true, 
                        SeriesRefreshed = 0, 
                        Message = $"No episodes found in calendar from past {daysBack} day{(daysBack > 1 ? "s" : "")}" 
                    };
                }

                // Get unique series IDs from prior episodes
                var uniqueSeriesIds = priorEpisodes.Select(e => e.SeriesId).Distinct().ToList();
                LogDebug($"Found {uniqueSeriesIds.Count} unique series in calendar from past {daysBack} days");

                int successCount = 0;
                var failedSeries = new List<string>();

                // Refresh each series
                foreach (var seriesId in uniqueSeriesIds)
                {
                    try
                    {
                        var success = await RefreshSeriesAsync(seriesId);
                        if (success)
                        {
                            successCount++;
                            var seriesTitle = priorEpisodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                            LogDebug($"Successfully refreshed series from prior days: {seriesTitle}");
                        }
                        else
                        {
                            var seriesTitle = priorEpisodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                            failedSeries.Add(seriesTitle);
                            LogError($"Failed to refresh series from prior days: {seriesTitle}", new Exception("Refresh command failed"));
                        }

                        // Small delay between requests to avoid overwhelming the API
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        var seriesTitle = priorEpisodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                        failedSeries.Add(seriesTitle);
                        LogError($"Exception refreshing series from prior days: {seriesTitle}", ex);
                    }
                }

                var dayText = daysBack == 1 ? "day" : "days";
                var message = $"Refreshed {successCount} of {uniqueSeriesIds.Count} series from past {daysBack} {dayText}";
                if (failedSeries.Any())
                {
                    message += $". Failed: {string.Join(", ", failedSeries)}";
                }

                LogDebug(message);
                return new RefreshCalendarResult 
                { 
                    Success = true, 
                    SeriesRefreshed = successCount, 
                    TotalSeries = uniqueSeriesIds.Count,
                    Message = message,
                    FailedSeries = failedSeries
                };
            }
            catch (Exception ex)
            {
                LogError($"Error refreshing calendar series from {daysBack} days back", ex);
                return new RefreshCalendarResult 
                { 
                    Success = false, 
                    SeriesRefreshed = 0, 
                    Message = $"Error: {ex.Message}" 
                };
            }
        }

        #endregion

        #region Logging

        private void LogDebug(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[SonarrService] {message}");
        }

        private void LogError(string message, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SonarrService] ERROR: {message}");
            System.Diagnostics.Debug.WriteLine($"[SonarrService] Exception: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[SonarrService] Stack trace: {ex.StackTrace}");
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
} 