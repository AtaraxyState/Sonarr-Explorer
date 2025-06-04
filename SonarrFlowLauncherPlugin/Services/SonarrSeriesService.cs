using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarrFlowLauncherPlugin.Models;

namespace SonarrFlowLauncherPlugin.Services
{
    public class SonarrSeriesService
    {
        private readonly ISonarrApiClient _apiClient;
        private readonly string _imageCache;

        // Constants
        private const string PosterCoverType = "poster";
        private const string ImageCacheDirName = "ImageCache";

        public SonarrSeriesService(ISonarrApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _imageCache = InitializeImageCache();
        }

        private string InitializeImageCache()
        {
            var assemblyLocation = Path.GetDirectoryName(typeof(Settings).Assembly.Location) ?? Environment.CurrentDirectory;
            var cacheDir = Path.Combine(assemblyLocation, ImageCacheDirName);
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }
            return cacheDir;
        }

        public async Task<List<SonarrSeries>> SearchSeriesAsync(string query)
        {
            try
            {
                var url = $"{_apiClient.BaseUrl}/series";
                _apiClient.LogDebug($"Fetching series from: {url}");

                var response = await _apiClient.HttpClient.GetStringAsync(url);
                var allSeries = JsonConvert.DeserializeObject<List<SonarrSeries>>(response) ?? new List<SonarrSeries>();
                
                _apiClient.LogDebug($"Retrieved {allSeries.Count} series");

                // Download posters for each series
                await DownloadPostersForSeriesAsync(allSeries);

                // Filter by query if provided
                return FilterSeries(allSeries, query);
            }
            catch (Exception ex)
            {
                _apiClient.LogError("Error searching series", ex);
                throw;
            }
        }

        public bool OpenSeriesInBrowser(string titleSlug)
        {
            try
            {
                var url = $"{_apiClient.WebBaseUrl}/series/{titleSlug}";
                return _apiClient.OpenUrlInBrowser(url);
            }
            catch (Exception ex)
            {
                _apiClient.LogError($"Error opening series {titleSlug} in browser", ex);
                return false;
            }
        }

        public async Task<bool> RefreshAllSeriesAsync()
        {
            try
            {
                var commandUrl = $"{_apiClient.BaseUrl}/command";
                var command = new { name = "RescanSeries" };
                var json = JsonConvert.SerializeObject(command);

                _apiClient.LogDebug($"Sending refresh all series command to: {commandUrl}");
                _apiClient.LogDebug($"Command payload: {json}");

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _apiClient.HttpClient.PostAsync(commandUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _apiClient.LogDebug("Refresh all series command sent successfully");
                    return true;
                }
                else
                {
                    _apiClient.LogError($"Failed to send refresh command. Status: {response.StatusCode}", new Exception(response.ReasonPhrase));
                    return false;
                }
            }
            catch (Exception ex)
            {
                _apiClient.LogError("Error refreshing all series", ex);
                return false;
            }
        }

        public async Task<bool> RefreshSeriesAsync(int seriesId)
        {
            try
            {
                var commandUrl = $"{_apiClient.BaseUrl}/command";
                var command = new { name = "RescanSeries", seriesId = seriesId };
                var json = JsonConvert.SerializeObject(command);

                _apiClient.LogDebug($"Sending refresh series command to: {commandUrl}");
                _apiClient.LogDebug($"Command payload: {json}");

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _apiClient.HttpClient.PostAsync(commandUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _apiClient.LogDebug($"Refresh series {seriesId} command sent successfully");
                    return true;
                }
                else
                {
                    _apiClient.LogError($"Failed to send refresh command for series {seriesId}. Status: {response.StatusCode}", new Exception(response.ReasonPhrase));
                    return false;
                }
            }
            catch (Exception ex)
            {
                _apiClient.LogError($"Error refreshing series {seriesId}", ex);
                return false;
            }
        }

        public async Task<string?> DownloadPosterAsync(int seriesId, string posterUrl)
        {
            try
            {
                var posterPath = Path.Combine(_imageCache, $"poster_{seriesId}.jpg");
                
                // Check cache first
                if (File.Exists(posterPath))
                    return posterPath;

                // Download and cache
                var imageBytes = await _apiClient.HttpClient.GetByteArrayAsync(posterUrl);
                await File.WriteAllBytesAsync(posterPath, imageBytes);
                
                return posterPath;
            }
            catch (Exception ex)
            {
                _apiClient.LogError($"Error downloading poster for series {seriesId}", ex);
                return null;
            }
        }

        public string? ExtractPosterUrl(IEnumerable<SonarrImage>? images)
        {
            var poster = images?.FirstOrDefault(i => i.CoverType == PosterCoverType);
            return poster != null ? 
                (!string.IsNullOrEmpty(poster.RemoteUrl) ? poster.RemoteUrl : poster.Url) : 
                null;
        }

        public string? ExtractPosterUrlFromRecord(dynamic images)
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
    }
} 