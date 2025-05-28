using Newtonsoft.Json;

namespace SonarrFlowLauncherPlugin.Models
{
    public class SonarrSeries
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

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