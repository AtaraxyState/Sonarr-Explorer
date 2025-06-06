using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Services;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;

namespace SonarrFlowLauncherPlugin.Commands
{
    public class LibrarySearchCommand : BaseCommand
    {
        public LibrarySearchCommand(SonarrService sonarrService, Settings settings) 
            : base(sonarrService, settings)
        {
        }

        public override string CommandFlag => "-l";
        public override string CommandName => "Search Sonarr Library";
        public override string CommandDescription => "Search for shows in your Sonarr library, or show all series when no search term is provided";

        public override List<Result> Execute(Query query)
        {
            if (!ValidateSettings())
            {
                return GetSettingsError();
            }

            var results = new List<Result>();
            var searchQuery = query.Search.StartsWith(CommandFlag) 
                ? query.Search.Substring(CommandFlag.Length).Trim() 
                : query.Search.Trim();

            try
            {
                System.Diagnostics.Debug.WriteLine($"Searching for: '{searchQuery}'");
                var series = SonarrService.SearchSeriesAsync(searchQuery).Result;
                System.Diagnostics.Debug.WriteLine($"Found {series.Count} results");

                if (series.Count == 0)
                {
                    var message = string.IsNullOrWhiteSpace(searchQuery) 
                        ? "No series found in your library" 
                        : $"No shows found matching '{searchQuery}'";
                    
                    results.Add(new Result
                    {
                        Title = "No Results Found",
                        SubTitle = message,
                        IcoPath = "Images\\icon.png",
                        Score = 100,
                        Action = _ => false
                    });
                }

                foreach (var show in series)
                {
                    System.Diagnostics.Debug.WriteLine($"Series: {show.Title}, Path: '{show.Path}', Exists: {(!string.IsNullOrEmpty(show.Path) ? System.IO.Directory.Exists(show.Path).ToString() : "N/A")}");
                    
                    var stats = show.Statistics ?? new Models.SeriesStatistics();
                    var episodeInfo = $"{stats.EpisodeFileCount}/{stats.TotalEpisodeCount} Episodes";
                    if (stats.TotalEpisodeCount == 0)
                    {
                        episodeInfo = "No Episodes";
                    }

                    var hasLocalPath = !string.IsNullOrEmpty(show.Path) && System.IO.Directory.Exists(show.Path);
                    
                    var subTitle = hasLocalPath 
                        ? $"{show.Network} | {show.Status} | {stats.SeasonCount} Seasons | {episodeInfo} | 📁 Right-click for files"
                        : $"{show.Network} | {show.Status} | {stats.SeasonCount} Seasons | {episodeInfo}";

                    results.Add(new Result
                    {
                        Title = show.Title,
                        SubTitle = subTitle,
                        IcoPath = !string.IsNullOrEmpty(show.PosterPath) ? show.PosterPath : "Images\\icon.png",
                        Score = 100,
                        Action = _ => SonarrService.OpenSeriesInBrowser(show.TitleSlug),
                        ContextData = show,
                        ActionKeywordAssigned = query.ActionKeyword
                    });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                System.Diagnostics.Debug.WriteLine($"Error in Query: {errorMessage}");
                
                results.Add(new Result
                {
                    Title = "Error Connecting to Sonarr",
                    SubTitle = $"Error: {errorMessage}",
                    IcoPath = "Images\\icon.png",
                    Score = 100,
                    Action = _ => false
                });
            }

            return results;
        }
    }
} 