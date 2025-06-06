using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarrFlowLauncherPlugin.Models;

namespace SonarrFlowLauncherPlugin.Services
{
    /// <summary>
    /// Service for managing Sonarr health checks and system status monitoring.
    /// Provides functionality to fetch health issues and trigger health check re-tests.
    /// </summary>
    public class SonarrHealthService
    {
        private readonly ISonarrApiClient _apiClient;

        /// <summary>
        /// Initializes a new instance of the SonarrHealthService.
        /// </summary>
        /// <param name="apiClient">The API client for Sonarr communication</param>
        public SonarrHealthService(ISonarrApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        /// <summary>
        /// Retrieves all current health check issues from Sonarr.
        /// </summary>
        /// <returns>List of health check issues</returns>
        public async Task<List<SonarrHealthCheck>> GetHealthChecksAsync()
        {
            try
            {
                var healthUrl = $"{_apiClient.BaseUrl}/health";
                var response = await _apiClient.HttpClient.GetStringAsync(healthUrl);
                
                _apiClient.LogDebug("Health API Response received");
                
                var healthChecks = JsonConvert.DeserializeObject<List<SonarrHealthCheck>>(response) ?? new List<SonarrHealthCheck>();
                
                _apiClient.LogDebug($"Retrieved {healthChecks.Count} health check issues");
                return healthChecks;
            }
            catch (Exception ex)
            {
                _apiClient.LogError("Error fetching health checks", ex);
                throw;
            }
        }

        /// <summary>
        /// Triggers a complete health check re-test by sending a CheckHealth command to Sonarr.
        /// </summary>
        /// <returns>True if the command was sent successfully, false otherwise</returns>
        public async Task<bool> TriggerHealthCheckAsync()
        {
            try
            {
                var commandUrl = $"{_apiClient.BaseUrl}/command";
                var command = new { name = "CheckHealth" };
                var json = JsonConvert.SerializeObject(command);

                _apiClient.LogDebug($"Sending health check command to: {commandUrl}");
                _apiClient.LogDebug($"Command payload: {json}");

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _apiClient.HttpClient.PostAsync(commandUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _apiClient.LogDebug("Health check command sent successfully");
                    return true;
                }
                else
                {
                    _apiClient.LogError($"Failed to send health check command. Status: {response.StatusCode}", new Exception(response.ReasonPhrase));
                    return false;
                }
            }
            catch (Exception ex)
            {
                _apiClient.LogError("Error triggering health check", ex);
                return false;
            }
        }

        /// <summary>
        /// Opens the Sonarr system status page in the default browser.
        /// </summary>
        /// <returns>True if the browser was opened successfully, false otherwise</returns>
        public bool OpenSystemStatusInBrowser()
        {
            try
            {
                var url = $"{_apiClient.WebBaseUrl}/system/status";
                return _apiClient.OpenUrlInBrowser(url);
            }
            catch (Exception ex)
            {
                _apiClient.LogError("Error opening system status in browser", ex);
                return false;
            }
        }

        /// <summary>
        /// Checks basic connectivity to Sonarr using the /ping endpoint.
        /// </summary>
        /// <returns>True if ping is successful, false otherwise</returns>
        public async Task<bool> PingAsync()
        {
            try
            {
                // Use the base URL without /api/v3 for the ping endpoint
                var baseWebUrl = _apiClient.WebBaseUrl;
                var pingUrl = $"{baseWebUrl}/ping";
                
                var response = await _apiClient.HttpClient.GetStringAsync(pingUrl);
                _apiClient.LogDebug("Ping endpoint responded successfully");
                return true;
            }
            catch (Exception ex)
            {
                _apiClient.LogError("Ping endpoint failed", ex);
                return false;
            }
        }

        /// <summary>
        /// Triggers specific health check tests based on the health check source.
        /// This is a best-effort attempt to re-test specific issues.
        /// </summary>
        /// <param name="healthCheck">The health check to re-test</param>
        /// <returns>True if a relevant command was sent, false otherwise</returns>
        public async Task<bool> RetestHealthCheckAsync(SonarrHealthCheck healthCheck)
        {
            try
            {
                string commandName = healthCheck.Source.ToLower() switch
                {
                    var s when s.Contains("download") => "CheckHealth", // General health check for download issues
                    var s when s.Contains("indexer") => "CheckHealth", // General health check for indexer issues
                    var s when s.Contains("root") || s.Contains("folder") => "CheckHealth", // General health check for folder issues
                    var s when s.Contains("update") => "ApplicationCheckUpdate", // Specific command for update checks
                    _ => "CheckHealth" // Default to general health check
                };

                var commandUrl = $"{_apiClient.BaseUrl}/command";
                var command = new { name = commandName };
                var json = JsonConvert.SerializeObject(command);

                _apiClient.LogDebug($"Sending specific health retest command: {commandName} for source: {healthCheck.Source}");

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _apiClient.HttpClient.PostAsync(commandUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _apiClient.LogDebug($"Health retest command sent successfully for: {healthCheck.Source}");
                    return true;
                }
                else
                {
                    _apiClient.LogError($"Failed to send health retest command. Status: {response.StatusCode}", new Exception(response.ReasonPhrase));
                    return false;
                }
            }
            catch (Exception ex)
            {
                _apiClient.LogError($"Error retesting health check for {healthCheck.Source}", ex);
                return false;
            }
        }
    }
} 