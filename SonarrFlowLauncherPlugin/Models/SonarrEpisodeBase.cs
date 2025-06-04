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
        
        // Series information for context menus
        public string SeriesTitle { get; set; } = string.Empty;
        public string SeriesPath { get; set; } = string.Empty;
        public string TitleSlug { get; set; } = string.Empty;
        
        // Episode file information for context menus
        public int EpisodeFileId { get; set; }
        public string EpisodeFilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
    }
} 