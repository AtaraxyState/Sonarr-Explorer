using System;
using System.Diagnostics;
using System.Net.Http;

namespace SonarrFlowLauncherPlugin.Services
{
    public class SonarrApiClient : ISonarrApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly Settings _settings;
        private bool _disposed;

        public SonarrApiClient(Settings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _httpClient = CreateHttpClient(settings);
        }

        public string BaseUrl => $"{(_settings.UseHttps ? "https" : "http")}://{_settings.ServerUrl.TrimEnd('/')}/api/v3";
        public string WebBaseUrl => $"{(_settings.UseHttps ? "https" : "http")}://{_settings.ServerUrl.TrimEnd('/')}";
        public HttpClient HttpClient => _httpClient;
        public Settings Settings => _settings;

        private HttpClient CreateHttpClient(Settings settings)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", settings.ApiKey);
            return client;
        }

        public void LogDebug(string message)
        {
            Debug.WriteLine($"[SonarrService] {message}");
        }

        public void LogError(string message, Exception ex)
        {
            Debug.WriteLine($"[SonarrService] ERROR: {message}");
            if (ex != null)
            {
                Debug.WriteLine($"[SonarrService] Exception: {ex.Message}");
                Debug.WriteLine($"[SonarrService] StackTrace: {ex.StackTrace}");
            }
        }

        public bool OpenUrlInBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error opening URL in browser: {url}", ex);
                return false;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
} 