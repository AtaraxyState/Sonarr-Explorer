using System;

namespace SonarrFlowLauncherPlugin.Models
{
    public class SonarrQueueItem : SonarrEpisodeBase
    {
        public string Status { get; set; } = string.Empty;
        public double Progress { get; set; }
        public DateTime? EstimatedCompletionTime { get; set; }
        public string Protocol { get; set; } = string.Empty;
        public string DownloadClient { get; set; } = string.Empty;
    }

    public class SonarrHistoryItem : SonarrEpisodeBase
    {
        public string EventType { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    public class SonarrActivity
    {
        public List<SonarrQueueItem> Queue { get; set; } = new();
        public List<SonarrHistoryItem> History { get; set; } = new();
    }
} 