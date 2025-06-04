using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarrFlowLauncherPlugin.Models;

namespace SonarrFlowLauncherPlugin.Services
{
    public class SonarrActivityService
    {
        private readonly ISonarrApiClient _apiClient;
        private readonly SonarrSeriesService _seriesService;

        public SonarrActivityService(ISonarrApiClient apiClient, SonarrSeriesService seriesService)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _seriesService = seriesService ?? throw new ArgumentNullException(nameof(seriesService));
        }

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
                _apiClient.LogError("Error getting activity", ex);
                throw;
            }
        }

        public bool OpenActivityInBrowser()
        {
            try
            {
                var url = $"{_apiClient.WebBaseUrl}/activity";
                return _apiClient.OpenUrlInBrowser(url);
            }
            catch (Exception ex)
            {
                _apiClient.LogError("Error opening activity in browser", ex);
                return false;
            }
        }

        private async Task<List<SonarrQueueItem>> FetchQueueItemsAsync()
        {
            var queueUrl = $"{_apiClient.BaseUrl}/queue?sortKey=timeleft&sortDir=asc&includeEpisode=true&includeSeries=true";
            var response = await _apiClient.HttpClient.GetStringAsync(queueUrl);
            
            _apiClient.LogDebug("Queue API Response received");
            
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

        private async Task<List<SonarrHistoryItem>> FetchHistoryItemsAsync()
        {
            var historyUrl = $"{_apiClient.BaseUrl}/history?sortKey=date&sortDir=desc&includeSeries=true&includeEpisode=true";
            var response = await _apiClient.HttpClient.GetStringAsync(historyUrl);
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
                DownloadClient = record.downloadClient ?? string.Empty,
                
                // Populate series information for context menus
                SeriesTitle = record.series?.title ?? string.Empty,
                SeriesPath = record.series?.path ?? string.Empty,
                TitleSlug = record.series?.titleSlug ?? string.Empty,
                
                // Populate episode file information if available
                EpisodeFileId = record.episode?.episodeFileId ?? 0,
                RelativePath = record.episode?.relativePath ?? string.Empty
            };

            // Construct full episode file path if we have series path and relative path
            if (!string.IsNullOrEmpty(queueItem.SeriesPath) && !string.IsNullOrEmpty(queueItem.RelativePath))
            {
                queueItem.EpisodeFilePath = System.IO.Path.Combine(queueItem.SeriesPath, queueItem.RelativePath);
            }

            // Download poster if available
            var posterUrl = _seriesService.ExtractPosterUrlFromRecord(record.series?.images);
            if (!string.IsNullOrEmpty(posterUrl))
            {
                queueItem.PosterPath = await _seriesService.DownloadPosterAsync(queueItem.SeriesId, posterUrl);
            }

            return queueItem;
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
                Date = record.date,
                
                // Populate series information for context menus
                SeriesTitle = record.series?.title ?? record.sourceTitle ?? string.Empty,
                SeriesPath = record.series?.path ?? string.Empty,
                TitleSlug = record.series?.titleSlug ?? string.Empty,
                
                // Populate episode file information if available
                EpisodeFileId = record.episodeFile?.id ?? record.episode?.episodeFileId ?? 0,
                RelativePath = record.episodeFile?.relativePath ?? record.episode?.relativePath ?? string.Empty
            };

            // Construct full episode file path if we have series path and relative path
            if (!string.IsNullOrEmpty(historyItem.SeriesPath) && !string.IsNullOrEmpty(historyItem.RelativePath))
            {
                historyItem.EpisodeFilePath = System.IO.Path.Combine(historyItem.SeriesPath, historyItem.RelativePath);
            }

            // Download poster if available
            var posterUrl = _seriesService.ExtractPosterUrlFromRecord(record.series?.images);
            if (!string.IsNullOrEmpty(posterUrl))
            {
                historyItem.PosterPath = await _seriesService.DownloadPosterAsync(historyItem.SeriesId, posterUrl);
            }

            return historyItem;
        }

        private double CalculateProgress(dynamic record)
        {
            try
            {
                // Try to get size information
                double totalSize = 0;
                double remainingSize = 0;

                if (record.size != null)
                {
                    totalSize = (double)(record.size ?? 0);
                }

                if (record.sizeleft != null)
                {
                    remainingSize = (double)(record.sizeleft ?? 0);
                }

                if (totalSize > 0 && remainingSize >= 0)
                {
                    double downloaded = totalSize - remainingSize;
                    double progress = (downloaded / totalSize) * 100;
                    return Math.Min(100, Math.Max(0, progress)); // Clamp between 0-100
                }

                // Fallback: if no size info, check status
                string status = record.status?.ToString()?.ToLower() ?? "";
                switch (status)
                {
                    case "completed":
                        return 100;
                    case "downloading":
                        return 50; // Unknown progress, assume halfway
                    case "queued":
                        return 0;
                    default:
                        return 0;
                }
            }
            catch (Exception ex)
            {
                _apiClient.LogError("Error calculating progress", ex);
                return 0;
            }
        }
    }
} 