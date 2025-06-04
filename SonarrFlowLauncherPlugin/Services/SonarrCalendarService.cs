using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarrFlowLauncherPlugin.Models;

namespace SonarrFlowLauncherPlugin.Services
{
    public class SonarrCalendarService
    {
        private readonly ISonarrApiClient _apiClient;
        private readonly SonarrSeriesService _seriesService;

        public SonarrCalendarService(ISonarrApiClient apiClient, SonarrSeriesService seriesService)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _seriesService = seriesService ?? throw new ArgumentNullException(nameof(seriesService));
        }

        public async Task<List<SonarrCalendarItem>> GetCalendarAsync(DateTime? start = null, DateTime? end = null)
        {
            try
            {
                start ??= DateTime.Today;
                end ??= DateTime.Today.AddDays(7);

                var url = $"{_apiClient.BaseUrl}/calendar?start={start:yyyy-MM-dd HH:mm:ss}&end={end:yyyy-MM-dd HH:mm:ss}&includeSeries=true";
                _apiClient.LogDebug($"Fetching calendar from: {url}");
                
                var response = await _apiClient.HttpClient.GetStringAsync(url);
                var calendarData = JsonConvert.DeserializeObject<List<dynamic>>(response);
                
                _apiClient.LogDebug($"Retrieved {calendarData?.Count ?? 0} calendar items");

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
                            _apiClient.LogError($"Error processing calendar item: {JsonConvert.SerializeObject(item)}", ex);
                        }
                    }
                }

                return calendarItems;
            }
            catch (Exception ex)
            {
                _apiClient.LogError("Error getting calendar", ex);
                throw;
            }
        }

        public bool OpenCalendarInBrowser()
        {
            try
            {
                var url = $"{_apiClient.WebBaseUrl}/calendar";
                return _apiClient.OpenUrlInBrowser(url);
            }
            catch (Exception ex)
            {
                _apiClient.LogError("Error opening calendar in browser", ex);
                return false;
            }
        }

        public async Task<RefreshCalendarResult> RefreshTodaysCalendarSeriesAsync()
        {
            return await RefreshCalendarSeriesAsync(DateTime.Today, DateTime.Today.AddDays(1), "today's calendar");
        }

        public async Task<RefreshCalendarResult> RefreshYesterdayCalendarSeriesAsync()
        {
            var yesterday = DateTime.Today.AddDays(-1);
            return await RefreshCalendarSeriesAsync(yesterday, DateTime.Today, "yesterday's calendar");
        }

        public async Task<RefreshCalendarResult> RefreshPriorDaysCalendarSeriesAsync(int daysBack)
        {
            if (daysBack < 1)
            {
                return new RefreshCalendarResult 
                { 
                    Success = false, 
                    SeriesRefreshed = 0, 
                    Message = "Days back must be 1 or greater" 
                };
            }

            var startDate = DateTime.Today.AddDays(-daysBack);
            var dayText = daysBack == 1 ? "day" : "days";
            return await RefreshCalendarSeriesAsync(startDate, DateTime.Today, $"past {daysBack} {dayText}");
        }

        public async Task<RefreshCalendarResult> RefreshOverdueCalendarSeriesAsync()
        {
            try
            {
                _apiClient.LogDebug("Starting refresh of overdue calendar series");
                
                // Get today's calendar entries
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var todaysEpisodes = await GetCalendarAsync(today, tomorrow);

                if (!todaysEpisodes.Any())
                {
                    _apiClient.LogDebug("No episodes found in today's calendar");
                    return new RefreshCalendarResult 
                    { 
                        Success = true, 
                        SeriesRefreshed = 0, 
                        Message = "No episodes found in today's calendar" 
                    };
                }

                var now = DateTime.Now;
                _apiClient.LogDebug($"Current local time: {now:yyyy-MM-dd HH:mm:ss}");
                _apiClient.LogDebug($"Found {todaysEpisodes.Count} episodes in today's calendar");

                // Filter for episodes that have already aired (with buffer)
                var bufferMinutes = 10; // Allow 10 minutes buffer after air time
                var overdueEpisodes = new List<SonarrCalendarItem>();

                foreach (var episode in todaysEpisodes)
                {
                    DateTime episodeAirTime;
                    
                    if (episode.AirDate.Kind == DateTimeKind.Utc)
                    {
                        // Convert UTC to local time
                        episodeAirTime = episode.AirDate.ToLocalTime();
                    }
                    else if (episode.AirDate.Kind == DateTimeKind.Local)
                    {
                        episodeAirTime = episode.AirDate;
                    }
                    else
                    {
                        // Assume local time if unspecified
                        episodeAirTime = episode.AirDate;
                    }
                    
                    var timeUntilAir = episodeAirTime.AddMinutes(bufferMinutes) - now;
                    var isOverdue = timeUntilAir.TotalMinutes <= 0;
                    
                    _apiClient.LogDebug($"Episode: {episode.SeriesTitle} S{episode.SeasonNumber:D2}E{episode.EpisodeNumber:D2} - Air Time: {episodeAirTime:yyyy-MM-dd HH:mm:ss} - Time Until Air (+{bufferMinutes}m buffer): {timeUntilAir.TotalMinutes:F1} minutes - Overdue: {isOverdue}");
                    
                    if (isOverdue)
                    {
                        overdueEpisodes.Add(episode);
                    }
                }

                if (!overdueEpisodes.Any())
                {
                    _apiClient.LogDebug("No overdue episodes found in today's calendar");
                    return new RefreshCalendarResult 
                    { 
                        Success = true, 
                        SeriesRefreshed = 0, 
                        Message = $"No overdue episodes found in today's calendar (checked {todaysEpisodes.Count} episodes)" 
                    };
                }

                // Get unique series IDs from overdue episodes
                var uniqueSeriesIds = overdueEpisodes.Select(e => e.SeriesId).Distinct().ToList();
                _apiClient.LogDebug($"Found {uniqueSeriesIds.Count} unique series with {overdueEpisodes.Count} overdue episodes");

                int successCount = 0;
                var failedSeries = new List<string>();

                // Refresh each series with overdue episodes
                foreach (var seriesId in uniqueSeriesIds)
                {
                    try
                    {
                        var success = await _seriesService.RefreshSeriesAsync(seriesId);
                        if (success)
                        {
                            successCount++;
                            var seriesTitle = overdueEpisodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                            _apiClient.LogDebug($"Successfully refreshed overdue series: {seriesTitle}");
                        }
                        else
                        {
                            var seriesTitle = overdueEpisodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                            failedSeries.Add(seriesTitle);
                            _apiClient.LogError($"Failed to refresh overdue series: {seriesTitle}", new Exception("Refresh command failed"));
                        }

                        // Small delay between requests to avoid overwhelming the API
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        var seriesTitle = overdueEpisodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                        failedSeries.Add(seriesTitle);
                        _apiClient.LogError($"Exception refreshing overdue series: {seriesTitle}", ex);
                    }
                }

                var message = $"Refreshed {successCount} of {uniqueSeriesIds.Count} series with {overdueEpisodes.Count} overdue episodes from today's calendar";
                if (failedSeries.Any())
                {
                    message += $". Failed: {string.Join(", ", failedSeries)}";
                }

                _apiClient.LogDebug(message);
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
                _apiClient.LogError("Error refreshing overdue calendar series", ex);
                return new RefreshCalendarResult 
                { 
                    Success = false, 
                    SeriesRefreshed = 0, 
                    Message = $"Error: {ex.Message}" 
                };
            }
        }

        private async Task<RefreshCalendarResult> RefreshCalendarSeriesAsync(DateTime startDate, DateTime endDate, string description)
        {
            try
            {
                _apiClient.LogDebug($"Starting refresh of {description} series");
                
                var episodes = await GetCalendarAsync(startDate, endDate);

                if (!episodes.Any())
                {
                    _apiClient.LogDebug($"No episodes found in {description}");
                    return new RefreshCalendarResult 
                    { 
                        Success = true, 
                        SeriesRefreshed = 0, 
                        Message = $"No episodes found in {description}" 
                    };
                }

                // Get unique series IDs from episodes
                var uniqueSeriesIds = episodes.Select(e => e.SeriesId).Distinct().ToList();
                _apiClient.LogDebug($"Found {uniqueSeriesIds.Count} unique series in {description}");

                int successCount = 0;
                var failedSeries = new List<string>();

                // Refresh each series
                foreach (var seriesId in uniqueSeriesIds)
                {
                    try
                    {
                        var success = await _seriesService.RefreshSeriesAsync(seriesId);
                        if (success)
                        {
                            successCount++;
                            var seriesTitle = episodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                            _apiClient.LogDebug($"Successfully refreshed series: {seriesTitle}");
                        }
                        else
                        {
                            var seriesTitle = episodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                            failedSeries.Add(seriesTitle);
                            _apiClient.LogError($"Failed to refresh series: {seriesTitle}", new Exception("Refresh command failed"));
                        }

                        // Small delay between requests to avoid overwhelming the API
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        var seriesTitle = episodes.FirstOrDefault(e => e.SeriesId == seriesId)?.SeriesTitle ?? $"Series {seriesId}";
                        failedSeries.Add(seriesTitle);
                        _apiClient.LogError($"Exception refreshing series: {seriesTitle}", ex);
                    }
                }

                var message = $"Refreshed {successCount} of {uniqueSeriesIds.Count} series from {description}";
                if (failedSeries.Any())
                {
                    message += $". Failed: {string.Join(", ", failedSeries)}";
                }

                _apiClient.LogDebug(message);
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
                _apiClient.LogError($"Error refreshing {description} series", ex);
                return new RefreshCalendarResult 
                { 
                    Success = false, 
                    SeriesRefreshed = 0, 
                    Message = $"Error: {ex.Message}" 
                };
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
                Overview = item.overview ?? string.Empty,
                
                // Add series information for context menu functionality
                SeriesPath = item.series?.path ?? string.Empty,
                TitleSlug = item.series?.titleSlug ?? string.Empty
            };

            // Download poster if available
            var posterUrl = _seriesService.ExtractPosterUrlFromRecord(item.series?.images);
            if (!string.IsNullOrEmpty(posterUrl))
            {
                calendarItem.PosterPath = await _seriesService.DownloadPosterAsync(calendarItem.SeriesId, posterUrl);
            }

            _apiClient.LogDebug($"Processed calendar item: {calendarItem.Title} S{calendarItem.SeasonNumber:D2}E{calendarItem.EpisodeNumber:D2} - Air Date: {calendarItem.AirDate:yyyy-MM-dd HH:mm:ss} ({calendarItem.AirDate.Kind})");
            return calendarItem;
        }

        private DateTime ParseAirDate(dynamic airDateValue)
        {
            try
            {
                if (airDateValue == null)
                {
                    _apiClient.LogDebug("Air date is null, using DateTime.MinValue");
                    return DateTime.MinValue;
                }

                // Convert the dynamic value to string first
                string airDateString = airDateValue.ToString();
                _apiClient.LogDebug($"Raw air date from API: {airDateString}");

                // Try to parse the datetime string
                if (DateTime.TryParse(airDateString, out DateTime parsedDate))
                {
                    _apiClient.LogDebug($"Parsed air date: {parsedDate:yyyy-MM-dd HH:mm:ss} (Kind: {parsedDate.Kind})");
                    
                    // Sonarr typically returns UTC times, so ensure they're marked as UTC
                    if (parsedDate.Kind == DateTimeKind.Unspecified)
                    {
                        // Assume UTC if not specified (common for Sonarr API)
                        parsedDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                        _apiClient.LogDebug($"Specified as UTC: {parsedDate:yyyy-MM-dd HH:mm:ss} ({parsedDate.Kind})");
                    }
                    
                    return parsedDate;
                }
                else
                {
                    _apiClient.LogError($"Failed to parse air date string: {airDateString}", new Exception("DateTime parsing failed"));
                    return DateTime.MinValue;
                }
            }
            catch (Exception ex)
            {
                _apiClient.LogError($"Error parsing air date: {airDateValue}", ex);
                return DateTime.MinValue;
            }
        }
    }
} 