using System.Windows.Controls;
using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Services;

namespace SonarrFlowLauncherPlugin
{
    public class Main : IPlugin, ISettingProvider
    {
        private PluginInitContext _context;
        private readonly Settings _settings;
        private readonly SonarrService _sonarrService;
        private readonly SettingsControl _settingsControl;

        public Main()
        {
            _settings = Settings.Load();
            _sonarrService = new SonarrService(_settings);
            _settingsControl = new SettingsControl(_settings);
        }

        public void Init(PluginInitContext context)
        {
            _context = context;
        }

        public List<Result> Query(Query query)
        {
            if (string.IsNullOrEmpty(_settings.ApiKey))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Sonarr API Key Not Set",
                        SubTitle = "Please set your Sonarr API key in the plugin settings",
                        IcoPath = "Images\\icon.png",
                        Action = _ => false
                    }
                };
            }

            if (string.IsNullOrEmpty(query.Search))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Search Sonarr",
                        SubTitle = "Type to search your Sonarr library",
                        IcoPath = "Images\\icon.png",
                        Action = _ => false
                    }
                };
            }

            var results = new List<Result>();
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"Searching for: {query.Search}");
                var series = _sonarrService.SearchSeriesAsync(query.Search).Result;
                System.Diagnostics.Debug.WriteLine($"Found {series.Count} results");

                if (series.Count == 0)
                {
                    results.Add(new Result
                    {
                        Title = "No Results Found",
                        SubTitle = $"No shows found matching '{query.Search}'",
                        IcoPath = "Images\\icon.png",
                        Score = 100,
                        Action = _ => false
                    });
                }

                foreach (var show in series)
                {
                    var stats = show.Statistics ?? new Models.SeriesStatistics();
                    var episodeInfo = $"{stats.EpisodeFileCount}/{stats.TotalEpisodeCount} Episodes";
                    if (stats.TotalEpisodeCount == 0)
                    {
                        episodeInfo = "No Episodes";
                    }

                    results.Add(new Result
                    {
                        Title = show.Title,
                        SubTitle = $"{show.Network} | {show.Status} | {stats.SeasonCount} Seasons | {episodeInfo}",
                        IcoPath = !string.IsNullOrEmpty(show.PosterPath) ? show.PosterPath : "Images\\icon.png",
                        Score = 100,
                        Action = _ =>
                        {
                            return _sonarrService.OpenSeriesInBrowser(show.Id).Result;
                        }
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

        public Control CreateSettingPanel()
        {
            return _settingsControl;
        }
    }
} 