using System;

namespace SonarrFlowLauncherPlugin.Models
{
    public abstract class SonarrEpisodeBase
    {
        public int Id { get; set; }
        public int SeriesId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public string Quality { get; set; } = string.Empty;
        public string PosterPath { get; set; } = string.Empty;
    }
} 