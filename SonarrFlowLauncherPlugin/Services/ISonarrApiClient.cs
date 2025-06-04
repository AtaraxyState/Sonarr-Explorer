using System;
using System.Net.Http;

namespace SonarrFlowLauncherPlugin.Services
{
    public interface ISonarrApiClient : IDisposable
    {
        string BaseUrl { get; }
        string WebBaseUrl { get; }
        HttpClient HttpClient { get; }
        Settings Settings { get; }
        void LogDebug(string message);
        void LogError(string message, Exception ex);
        bool OpenUrlInBrowser(string url);
    }
} 