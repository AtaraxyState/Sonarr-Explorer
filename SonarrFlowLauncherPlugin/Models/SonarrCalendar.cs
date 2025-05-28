using System;

namespace SonarrFlowLauncherPlugin.Models
{
    public class SonarrCalendarItem : SonarrEpisodeBase
    {
        public string SeriesTitle { get; set; } = string.Empty;
        public DateTime AirDate { get; set; }
        public bool HasFile { get; set; }
        public bool Monitored { get; set; }
        public string Overview { get; set; } = string.Empty;
        public string EpisodeTitle { get; set; } = string.Empty;
    }
} 