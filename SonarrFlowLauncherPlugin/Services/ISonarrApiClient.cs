using System;
using System.Net.Http;

namespace SonarrFlowLauncherPlugin.Services
{
    /// <summary>
    /// Interface defining the contract for Sonarr API client implementations.
    /// Provides HTTP communication capabilities, URL generation, and utility methods for Sonarr integration.
    /// </summary>
    /// <remarks>
    /// This interface abstracts the HTTP communication layer enabling:
    /// - Testability through dependency injection and mocking
    /// - Consistent API base URL and web URL generation
    /// - Centralized logging and error handling
    /// - Resource management through IDisposable pattern
    /// </remarks>
    public interface ISonarrApiClient : IDisposable
    {
        /// <summary>
        /// Gets the fully qualified base URL for Sonarr API endpoints.
        /// Includes protocol (HTTP/HTTPS) and constructs URLs like "https://localhost:8989/api/v3/"
        /// </summary>
        string BaseUrl { get; }
        
        /// <summary>
        /// Gets the base URL for opening Sonarr web interface in browser.
        /// Used for constructing web UI links like "https://localhost:8989/series/breaking-bad"
        /// </summary>
        string WebBaseUrl { get; }
        
        /// <summary>
        /// Gets the configured HttpClient instance for making API requests.
        /// Pre-configured with authentication headers, timeouts, and base settings.
        /// </summary>
        HttpClient HttpClient { get; }
        
        /// <summary>
        /// Gets the plugin settings instance containing API configuration.
        /// Provides access to API key, server URL, and connection preferences.
        /// </summary>
        Settings Settings { get; }
        
        /// <summary>
        /// Logs debug information for troubleshooting and development purposes.
        /// </summary>
        /// <param name="message">Debug message to log</param>
        void LogDebug(string message);
        
        /// <summary>
        /// Logs error information with exception details for debugging failed operations.
        /// </summary>
        /// <param name="message">Error message describing the failure</param>
        /// <param name="ex">Exception that occurred</param>
        void LogError(string message, Exception ex);
        
        /// <summary>
        /// Opens the specified URL in the user's default web browser.
        /// </summary>
        /// <param name="url">Fully qualified URL to open</param>
        /// <returns>True if browser launched successfully, false if operation failed</returns>
        bool OpenUrlInBrowser(string url);
    }
} 