using Newtonsoft.Json;

namespace SonarrFlowLauncherPlugin.Models
{
    /// <summary>
    /// Represents a health check issue from Sonarr's health monitoring system.
    /// Maps to Sonarr API health endpoint responses.
    /// </summary>
    public class SonarrHealthCheck
    {
        /// <summary>
        /// Unique identifier for the health check type
        /// </summary>
        [JsonProperty("source")]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Type of health check (e.g., "warning", "error")
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable message describing the health issue
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// URL to documentation or help for this health check
        /// </summary>
        [JsonProperty("wikiUrl")]
        public string WikiUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets the icon for the health check based on its type
        /// </summary>
        public string GetIcon()
        {
            return Type.ToLower() switch
            {
                "error" => "❌",
                "warning" => "⚠️",
                "info" => "ℹ️",
                _ => "🔍"
            };
        }

        /// <summary>
        /// Gets the display title for the health check
        /// </summary>
        public string GetDisplayTitle()
        {
            return $"{GetIcon()} {Source}";
        }

        /// <summary>
        /// Gets the subtitle with message and type
        /// </summary>
        public string GetDisplaySubTitle()
        {
            return $"{Type.ToUpper()}: {Message}";
        }
    }
} 