using System.Collections.Generic;

namespace SonarrFlowLauncherPlugin.Models
{
    public class RefreshCalendarResult
    {
        public bool Success { get; set; }
        public int SeriesRefreshed { get; set; }
        public int TotalSeries { get; set; }
        public string Message { get; set; }
        public List<string> FailedSeries { get; set; } = new List<string>();
    }
} 