using Newtonsoft.Json;

namespace SonarrFlowLauncherPlugin.Models
{
    public class SonarrSeries
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("titleSlug")]
        public string TitleSlug { get; set; }

        [JsonProperty("overview")]
        public string Overview { get; set; }

        [JsonProperty("images")]
        public List<SonarrImage> Images { get; set; } = new();

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("network")]
        public string Network { get; set; }

        [JsonProperty("statistics")]
        public SeriesStatistics Statistics { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        public string PosterPath { get; set; }
    }

    public class SonarrEpisode
    {
        public int Id { get; set; }
        public int SeriesId { get; set; }
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? AirDate { get; set; }
        public bool Monitored { get; set; }
        public bool HasFile { get; set; }
        public string Overview { get; set; } = string.Empty;
        
        // File information
        public int EpisodeFileId { get; set; }
        public string Quality { get; set; } = string.Empty;
        public string Resolution { get; set; } = string.Empty;
        public List<string> Languages { get; set; } = new();
        public string RelativePath { get; set; } = string.Empty;
        public long Size { get; set; }
        
        // Computed properties
        public string SeriesPath { get; set; } = string.Empty;
        public string SeriesTitle { get; set; } = string.Empty;
        public string FullPath => !string.IsNullOrEmpty(SeriesPath) && !string.IsNullOrEmpty(RelativePath) 
            ? System.IO.Path.Combine(SeriesPath, RelativePath) 
            : string.Empty;
        public string DirectoryPath => !string.IsNullOrEmpty(FullPath) 
            ? System.IO.Path.GetDirectoryName(FullPath) ?? string.Empty 
            : string.Empty;
        
        // Status indicators
        public string StatusIcon => GetStatusIcon();
        public string StatusText => GetStatusText();
        
        private string GetStatusIcon()
        {
            if (!Monitored) return "👁️‍🗨️"; // Not monitored
            if (HasFile) return "✅"; // Downloaded
            if (AirDate.HasValue && AirDate.Value > DateTime.Now) return "📅"; // Future episode
            return "❌"; // Missing
        }
        
        private string GetStatusText()
        {
            if (!Monitored) return "Not Monitored";
            if (HasFile) return $"Downloaded ({Quality})";
            if (AirDate.HasValue && AirDate.Value > DateTime.Now) return $"Unaired ({AirDate.Value:MMM dd})";
            return "Missing";
        }
    }

    public class SonarrImage
    {
        [JsonProperty("coverType")]
        public string CoverType { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("remoteUrl")]
        public string RemoteUrl { get; set; }
    }

    public class SeriesStatistics
    {
        [JsonProperty("seasonCount")]
        public int SeasonCount { get; set; }

        [JsonProperty("episodeCount")]
        public int EpisodeCount { get; set; }

        [JsonProperty("episodeFileCount")]
        public int EpisodeFileCount { get; set; }

        [JsonProperty("totalEpisodeCount")]
        public int TotalEpisodeCount { get; set; }

        [JsonProperty("sizeOnDisk")]
        public long SizeOnDisk { get; set; }
    }
} 