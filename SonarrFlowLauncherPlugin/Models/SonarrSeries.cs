using Newtonsoft.Json;

namespace SonarrFlowLauncherPlugin.Models
{
    /// <summary>
    /// Represents a TV series in Sonarr with metadata, statistics, and file information.
    /// Maps to Sonarr API series objects with additional computed properties for plugin functionality.
    /// </summary>
    /// <remarks>
    /// Contains complete series information including:
    /// - Basic metadata (title, overview, status)
    /// - File system path for direct folder access
    /// - Statistics (episode counts, storage usage)
    /// - Image information for UI display
    /// - Computed poster path for Flow Launcher icons
    /// </remarks>
    public class SonarrSeries
    {
        /// <summary>
        /// Unique identifier for the series in Sonarr database
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Full title of the TV series as displayed in Sonarr
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// URL-friendly slug version of the title used for web links and API calls
        /// </summary>
        [JsonProperty("titleSlug")]
        public string TitleSlug { get; set; }

        /// <summary>
        /// Plot summary or description of the series
        /// </summary>
        [JsonProperty("overview")]
        public string Overview { get; set; }

        /// <summary>
        /// Collection of poster and banner images for the series
        /// </summary>
        [JsonProperty("images")]
        public List<SonarrImage> Images { get; set; } = new();

        /// <summary>
        /// Current status of the series (e.g., "continuing", "ended", "upcoming")
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// Television network or streaming service that airs the series
        /// </summary>
        [JsonProperty("network")]
        public string Network { get; set; }

        /// <summary>
        /// Statistical information about episodes, seasons, and storage
        /// </summary>
        [JsonProperty("statistics")]
        public SeriesStatistics Statistics { get; set; }

        /// <summary>
        /// File system path where series episodes are stored
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary>
        /// Computed path to the series poster image for use as Flow Launcher icon.
        /// Populated by processing the Images collection to find poster URLs.
        /// </summary>
        public string PosterPath { get; set; }
    }

    /// <summary>
    /// Represents a TV episode with detailed metadata, file information, and status indicators.
    /// Provides comprehensive episode data for calendar, activity, and file management features.
    /// </summary>
    /// <remarks>
    /// Includes computed properties for:
    /// - Status icons and text based on monitoring and download state
    /// - Full file paths combining series path and relative episode path
    /// - Quality and language information for downloaded episodes
    /// - Air date tracking for scheduling and overdue detection
    /// </remarks>
    public class SonarrEpisode
    {
        /// <summary>
        /// Unique identifier for the episode in Sonarr database
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Identifier of the parent series this episode belongs to
        /// </summary>
        public int SeriesId { get; set; }
        
        /// <summary>
        /// Season number (1-based) that this episode belongs to
        /// </summary>
        public int SeasonNumber { get; set; }
        
        /// <summary>
        /// Episode number within the season (1-based)
        /// </summary>
        public int EpisodeNumber { get; set; }
        
        /// <summary>
        /// Title of the specific episode
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Date and time when the episode originally aired or is scheduled to air
        /// </summary>
        public DateTime? AirDate { get; set; }
        
        /// <summary>
        /// Whether this episode is being monitored for automatic downloading
        /// </summary>
        public bool Monitored { get; set; }
        
        /// <summary>
        /// Whether the episode file has been downloaded and is available locally
        /// </summary>
        public bool HasFile { get; set; }
        
        /// <summary>
        /// Plot summary or description of the episode
        /// </summary>
        public string Overview { get; set; } = string.Empty;
        
        /// <summary>
        /// Identifier of the associated episode file in Sonarr (0 if no file)
        /// </summary>
        public int EpisodeFileId { get; set; }
        
        /// <summary>
        /// Quality profile of the downloaded episode file (e.g., "HDTV-720p", "Bluray-1080p")
        /// </summary>
        public string Quality { get; set; } = string.Empty;
        
        /// <summary>
        /// Video resolution of the downloaded file (e.g., "720p", "1080p", "4K")
        /// </summary>
        public string Resolution { get; set; } = string.Empty;
        
        /// <summary>
        /// List of audio languages available in the episode file
        /// </summary>
        public List<string> Languages { get; set; } = new();
        
        /// <summary>
        /// File path relative to the series folder (e.g., "Season 01/Episode.mkv")
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;
        
        /// <summary>
        /// File size in bytes of the downloaded episode
        /// </summary>
        public long Size { get; set; }
        
        /// <summary>
        /// Full path to the series folder (populated from parent series data)
        /// </summary>
        public string SeriesPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Title of the parent series (populated from parent series data)
        /// </summary>
        public string SeriesTitle { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets the complete file system path to the episode file by combining series path and relative path
        /// </summary>
        public string FullPath => !string.IsNullOrEmpty(SeriesPath) && !string.IsNullOrEmpty(RelativePath) 
            ? System.IO.Path.Combine(SeriesPath, RelativePath) 
            : string.Empty;
            
        /// <summary>
        /// Gets the directory path containing the episode file
        /// </summary>
        public string DirectoryPath => !string.IsNullOrEmpty(FullPath) 
            ? System.IO.Path.GetDirectoryName(FullPath) ?? string.Empty 
            : string.Empty;
        
        /// <summary>
        /// Gets a visual icon representing the episode's current status (downloaded, missing, unaired, etc.)
        /// </summary>
        public string StatusIcon => GetStatusIcon();
        
        /// <summary>
        /// Gets human-readable text describing the episode's current status and quality
        /// </summary>
        public string StatusText => GetStatusText();
        
        /// <summary>
        /// Determines the appropriate status icon based on monitoring state, file availability, and air date
        /// </summary>
        /// <returns>Emoji icon representing the episode status</returns>
        private string GetStatusIcon()
        {
            if (!Monitored) return "👁️‍🗨️"; // Not monitored
            if (HasFile) return "✅"; // Downloaded
            if (AirDate.HasValue && AirDate.Value > DateTime.Now) return "📅"; // Future episode
            return "❌"; // Missing
        }
        
        /// <summary>
        /// Generates descriptive status text including quality information for downloaded episodes
        /// </summary>
        /// <returns>Human-readable status description</returns>
        private string GetStatusText()
        {
            if (!Monitored) return "Not Monitored";
            if (HasFile) return $"Downloaded ({Quality})";
            if (AirDate.HasValue && AirDate.Value > DateTime.Now) return $"Unaired ({AirDate.Value:MMM dd})";
            return "Missing";
        }
    }

    /// <summary>
    /// Represents image metadata for series posters, banners, and fanart.
    /// Maps to Sonarr API image objects with cover type classification and URL information.
    /// </summary>
    public class SonarrImage
    {
        /// <summary>
        /// Type of image (e.g., "poster", "banner", "fanart")
        /// </summary>
        [JsonProperty("coverType")]
        public string CoverType { get; set; }

        /// <summary>
        /// Local URL path to the image on the Sonarr server
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// Original remote URL where the image was sourced from
        /// </summary>
        [JsonProperty("remoteUrl")]
        public string RemoteUrl { get; set; }
    }

    /// <summary>
    /// Statistical information about a TV series including episode counts and storage usage.
    /// Provides metrics for display in series listings and overview information.
    /// </summary>
    public class SeriesStatistics
    {
        /// <summary>
        /// Total number of seasons in the series
        /// </summary>
        [JsonProperty("seasonCount")]
        public int SeasonCount { get; set; }

        /// <summary>
        /// Total number of episodes that have aired
        /// </summary>
        [JsonProperty("episodeCount")]
        public int EpisodeCount { get; set; }

        /// <summary>
        /// Number of episodes that have been downloaded
        /// </summary>
        [JsonProperty("episodeFileCount")]
        public int EpisodeFileCount { get; set; }

        /// <summary>
        /// Total number of episodes including unaired future episodes
        /// </summary>
        [JsonProperty("totalEpisodeCount")]
        public int TotalEpisodeCount { get; set; }

        /// <summary>
        /// Total disk space used by downloaded episodes in bytes
        /// </summary>
        [JsonProperty("sizeOnDisk")]
        public long SizeOnDisk { get; set; }
    }
} 