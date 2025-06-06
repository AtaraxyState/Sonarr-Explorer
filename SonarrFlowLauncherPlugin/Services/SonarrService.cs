using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SonarrFlowLauncherPlugin.Models;

namespace SonarrFlowLauncherPlugin.Services
{
    /// <summary>
    /// Main Sonarr service that acts as a facade coordinating specialized services.
    /// Maintains backward compatibility while providing better separation of concerns.
    /// </summary>
    public class SonarrService : IDisposable
    {
        private readonly ISonarrApiClient _apiClient;
        private readonly SonarrSeriesService _seriesService;
        private readonly SonarrCalendarService _calendarService;
        private readonly SonarrActivityService _activityService;
        private readonly SonarrHealthService _healthService;
        private bool _disposed;

        public SonarrService(Settings settings)
        {
            _apiClient = new SonarrApiClient(settings);
            _seriesService = new SonarrSeriesService(_apiClient);
            _calendarService = new SonarrCalendarService(_apiClient, _seriesService);
            _activityService = new SonarrActivityService(_apiClient, _seriesService);
            _healthService = new SonarrHealthService(_apiClient);
        }

        #region Series Operations

        public async Task<List<SonarrSeries>> SearchSeriesAsync(string query)
            => await _seriesService.SearchSeriesAsync(query);

        public bool OpenSeriesInBrowser(string titleSlug)
            => _seriesService.OpenSeriesInBrowser(titleSlug);

        public async Task<bool> RefreshAllSeriesAsync()
            => await _seriesService.RefreshAllSeriesAsync();

        public async Task<bool> RefreshSeriesAsync(int seriesId)
            => await _seriesService.RefreshSeriesAsync(seriesId);

        #endregion

        #region Calendar Operations

        public async Task<List<SonarrCalendarItem>> GetCalendarAsync(DateTime? start = null, DateTime? end = null)
            => await _calendarService.GetCalendarAsync(start, end);

        public bool OpenCalendarInBrowser()
            => _calendarService.OpenCalendarInBrowser();

        public async Task<RefreshCalendarResult> RefreshTodaysCalendarSeriesAsync()
            => await _calendarService.RefreshTodaysCalendarSeriesAsync();

        public async Task<RefreshCalendarResult> RefreshYesterdayCalendarSeriesAsync()
            => await _calendarService.RefreshYesterdayCalendarSeriesAsync();

        public async Task<RefreshCalendarResult> RefreshOverdueCalendarSeriesAsync()
            => await _calendarService.RefreshOverdueCalendarSeriesAsync();

        public async Task<RefreshCalendarResult> RefreshPriorDaysCalendarSeriesAsync(int daysBack)
            => await _calendarService.RefreshPriorDaysCalendarSeriesAsync(daysBack);

        #endregion

        #region Activity Operations

        public async Task<SonarrActivity> GetActivityAsync()
            => await _activityService.GetActivityAsync();

        public bool OpenActivityInBrowser()
            => _activityService.OpenActivityInBrowser();

        #endregion

        #region Health Operations

        public async Task<List<SonarrHealthCheck>> GetHealthChecksAsync()
            => await _healthService.GetHealthChecksAsync();

        public async Task<bool> TriggerHealthCheckAsync()
            => await _healthService.TriggerHealthCheckAsync();

        public async Task<bool> RetestHealthCheckAsync(SonarrHealthCheck healthCheck)
            => await _healthService.RetestHealthCheckAsync(healthCheck);

        public bool OpenSystemStatusInBrowser()
            => _healthService.OpenSystemStatusInBrowser();

        public async Task<bool> PingAsync()
            => await _healthService.PingAsync();

        #endregion

        #region Utility Methods

        /// <summary>
        /// Provides access to the API client for advanced scenarios
        /// </summary>
        public ISonarrApiClient ApiClient => _apiClient;

        /// <summary>
        /// Provides access to the series service for advanced scenarios
        /// </summary>
        public SonarrSeriesService SeriesService => _seriesService;

        /// <summary>
        /// Provides access to the calendar service for advanced scenarios
        /// </summary>
        public SonarrCalendarService CalendarService => _calendarService;

        /// <summary>
        /// Provides access to the activity service for advanced scenarios
        /// </summary>
        public SonarrActivityService ActivityService => _activityService;

        /// <summary>
        /// Provides access to the health service for advanced scenarios
        /// </summary>
        public SonarrHealthService HealthService => _healthService;

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _apiClient?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
} 