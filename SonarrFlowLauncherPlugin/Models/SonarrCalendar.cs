using System;

namespace SonarrFlowLauncherPlugin.Models
{
    public class SonarrCalendarItem : SonarrEpisodeBase
    {
        public DateTime AirDate { get; set; }
        public bool HasFile { get; set; }
        public bool Monitored { get; set; }
        public string Overview { get; set; } = string.Empty;
        public string EpisodeTitle { get; set; } = string.Empty;
        public string Network { get; set; } = string.Empty;
    }
} 