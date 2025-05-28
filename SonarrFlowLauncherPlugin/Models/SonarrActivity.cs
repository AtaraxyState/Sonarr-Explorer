using System;

namespace SonarrFlowLauncherPlugin.Models
{
    public class SonarrQueueItem
    {
        public int Id { get; set; }
        public int SeriesId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public string Quality { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public double Progress { get; set; }
        public DateTime? EstimatedCompletionTime { get; set; }
        public string Protocol { get; set; } = string.Empty;
        public string DownloadClient { get; set; } = string.Empty;
    }

    public class SonarrHistoryItem
    {
        public int Id { get; set; }
        public int SeriesId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public string Quality { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    public class SonarrActivity
    {
        public List<SonarrQueueItem> Queue { get; set; } = new();
        public List<SonarrHistoryItem> History { get; set; } = new();
    }
} 