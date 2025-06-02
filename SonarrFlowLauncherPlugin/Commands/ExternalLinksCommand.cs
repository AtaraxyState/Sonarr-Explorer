using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Services;
using System.Diagnostics;
using System.Web;

namespace SonarrFlowLauncherPlugin.Commands
{
    public class ExternalLinksCommand : BaseCommand
    {
        public ExternalLinksCommand(SonarrService sonarrService, Settings settings) 
            : base(sonarrService, settings)
        {
        }

        public override string CommandFlag => "-link";
        public override string CommandName => "External Links";
        public override string CommandDescription => "Quick access to external sites and searches";

        public override List<Result> Execute(Query query)
        {
            var results = new List<Result>();
            
            var searchQuery = query.Search
                .Replace(CommandFlag, "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            // Handle direct command shortcuts
            if (query.Search.StartsWith("-tvdb", StringComparison.OrdinalIgnoreCase))
            {
                var tvdbSearch = query.Search.Replace("-tvdb", "").Trim();
                return GetTvdbResults(tvdbSearch);
            }
            else if (query.Search.StartsWith("-imdb", StringComparison.OrdinalIgnoreCase))
            {
                var imdbSearch = query.Search.Replace("-imdb", "").Trim();
                return GetImdbResults(imdbSearch);
            }
            else if (query.Search.StartsWith("-reddit", StringComparison.OrdinalIgnoreCase))
            {
                var redditSearch = query.Search.Replace("-reddit", "").Trim();
                return GetRedditResults(redditSearch);
            }

            // Parse command type and search term for -link commands
            var parts = searchQuery.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var commandType = parts.Length > 0 ? parts[0].ToLower() : "";
            var searchTerm = parts.Length > 1 ? parts[1] : "";

            if (string.IsNullOrWhiteSpace(commandType))
            {
                return GetDefaultLinksResults();
            }

            return commandType switch
            {
                "tvdb" => GetTvdbResults(searchTerm),
                "imdb" => GetImdbResults(searchTerm),
                "reddit" => GetRedditResults(searchTerm),
                "docs" => GetDocumentationResults(),
                "sonarr" => GetSonarrResults(),
                "github" => GetGithubResults(),
                _ => GetDefaultLinksResults()
            };
        }

        private List<Result> GetDefaultLinksResults()
        {
            var results = new List<Result>();

            results.Add(new Result
            {
                Title = "🔗 External Links & Searches",
                SubTitle = "Quick access to databases and community resources",
                IcoPath = "Images\\icon.png",
                Score = 100
            });

            // Database searches
            results.Add(new Result
            {
                Title = "📺 TVDB Search",
                SubTitle = "snr -link tvdb [series name] - Search TheTVDB",
                IcoPath = "Images\\icon.png",
                Score = 95,
                Action = _ => OpenUrl("https://thetvdb.com/")
            });

            results.Add(new Result
            {
                Title = "🎬 IMDB Search",
                SubTitle = "snr -link imdb [series name] - Search Internet Movie Database",
                IcoPath = "Images\\icon.png",
                Score = 94,
                Action = _ => OpenUrl("https://www.imdb.com/")
            });

            // Community links
            results.Add(new Result
            {
                Title = "🤖 r/Sonarr",
                SubTitle = "snr -link reddit - Open Sonarr subreddit",
                IcoPath = "Images\\icon.png",
                Score = 93,
                Action = _ => OpenUrl("https://www.reddit.com/r/sonarr/")
            });

            // Documentation
            results.Add(new Result
            {
                Title = "📖 Sonarr Wiki",
                SubTitle = "snr -link sonarr - Official Sonarr documentation",
                IcoPath = "Images\\icon.png",
                Score = 92,
                Action = _ => OpenUrl("https://wiki.servarr.com/sonarr")
            });

            results.Add(new Result
            {
                Title = "📚 Plugin Documentation",
                SubTitle = "snr -link docs - Plugin setup and usage guide",
                IcoPath = "Images\\icon.png",
                Score = 91,
                Action = _ => OpenUrl("https://github.com/AtaraxyState/Sonarr-Explorer/blob/main/README.md")
            });

            results.Add(new Result
            {
                Title = "💻 Plugin GitHub",
                SubTitle = "snr -link github - Source code and issues",
                IcoPath = "Images\\icon.png",
                Score = 90,
                Action = _ => OpenUrl("https://github.com/AtaraxyState/Sonarr-Explorer")
            });

            return results;
        }

        private List<Result> GetTvdbResults(string searchTerm)
        {
            var results = new List<Result>();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                results.Add(new Result
                {
                    Title = "📺 TheTVDB",
                    SubTitle = "The Television Database - Browse or search for series",
                    IcoPath = "Images\\icon.png",
                    Score = 100,
                    Action = _ => OpenUrl("https://thetvdb.com/")
                });
            }
            else
            {
                var encodedSearch = HttpUtility.UrlEncode(searchTerm);
                
                results.Add(new Result
                {
                    Title = $"📺 Search TVDB for '{searchTerm}'",
                    SubTitle = "Search TheTVDB for series information",
                    IcoPath = "Images\\icon.png",
                    Score = 100,
                    Action = _ => OpenUrl($"https://thetvdb.com/search?query={encodedSearch}")
                });

                results.Add(new Result
                {
                    Title = "📺 Browse TVDB",
                    SubTitle = "Open TheTVDB main page",
                    IcoPath = "Images\\icon.png",
                    Score = 95,
                    Action = _ => OpenUrl("https://thetvdb.com/")
                });
            }

            return results;
        }

        private List<Result> GetImdbResults(string searchTerm)
        {
            var results = new List<Result>();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                results.Add(new Result
                {
                    Title = "🎬 Internet Movie Database",
                    SubTitle = "IMDB - Browse movies and TV shows",
                    IcoPath = "Images\\icon.png",
                    Score = 100,
                    Action = _ => OpenUrl("https://www.imdb.com/")
                });
            }
            else
            {
                var encodedSearch = HttpUtility.UrlEncode(searchTerm);
                
                results.Add(new Result
                {
                    Title = $"🎬 Search IMDB for '{searchTerm}'",
                    SubTitle = "Search Internet Movie Database",
                    IcoPath = "Images\\icon.png",
                    Score = 100,
                    Action = _ => OpenUrl($"https://www.imdb.com/find?q={encodedSearch}&s=tt&ttype=tv")
                });

                results.Add(new Result
                {
                    Title = "🎬 Browse IMDB",
                    SubTitle = "Open IMDB main page",
                    IcoPath = "Images\\icon.png",
                    Score = 95,
                    Action = _ => OpenUrl("https://www.imdb.com/")
                });
            }

            return results;
        }

        private List<Result> GetRedditResults(string searchTerm)
        {
            var results = new List<Result>();

            results.Add(new Result
            {
                Title = "🤖 r/Sonarr",
                SubTitle = "Sonarr community subreddit - Help, tips, and discussions",
                IcoPath = "Images\\icon.png",
                Score = 100,
                Action = _ => OpenUrl("https://www.reddit.com/r/sonarr/")
            });

            results.Add(new Result
            {
                Title = "📱 r/usenet",
                SubTitle = "Usenet community and discussions",
                IcoPath = "Images\\icon.png",
                Score = 95,
                Action = _ => OpenUrl("https://www.reddit.com/r/usenet/")
            });

            results.Add(new Result
            {
                Title = "🏠 r/homelab",
                SubTitle = "Home lab and self-hosting community",
                IcoPath = "Images\\icon.png",
                Score = 94,
                Action = _ => OpenUrl("https://www.reddit.com/r/homelab/")
            });

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var encodedSearch = HttpUtility.UrlEncode(searchTerm);
                results.Insert(1, new Result
                {
                    Title = $"🔍 Search r/Sonarr for '{searchTerm}'",
                    SubTitle = "Search within the Sonarr subreddit",
                    IcoPath = "Images\\icon.png",
                    Score = 99,
                    Action = _ => OpenUrl($"https://www.reddit.com/r/sonarr/search?q={encodedSearch}&restrict_sr=1")
                });
            }

            return results;
        }

        private List<Result> GetDocumentationResults()
        {
            var results = new List<Result>();

            results.Add(new Result
            {
                Title = "📖 Plugin Documentation",
                SubTitle = "Setup guide, features, and troubleshooting",
                IcoPath = "Images\\icon.png",
                Score = 100,
                Action = _ => OpenUrl("https://github.com/AtaraxyState/Sonarr-Explorer/blob/main/README.md")
            });

            results.Add(new Result
            {
                Title = "📚 Sonarr Wiki",
                SubTitle = "Official Sonarr documentation",
                IcoPath = "Images\\icon.png",
                Score = 95,
                Action = _ => OpenUrl("https://wiki.servarr.com/sonarr")
            });

            results.Add(new Result
            {
                Title = "🔧 Sonarr API Docs",
                SubTitle = "Sonarr API documentation",
                IcoPath = "Images\\icon.png",
                Score = 94,
                Action = _ => OpenUrl("https://sonarr.tv/docs/api/")
            });

            results.Add(new Result
            {
                Title = "💡 Flow Launcher Docs",
                SubTitle = "Flow Launcher documentation and plugin development",
                IcoPath = "Images\\icon.png",
                Score = 93,
                Action = _ => OpenUrl("https://github.com/Flow-Launcher/Flow.Launcher/wiki")
            });

            return results;
        }

        private List<Result> GetSonarrResults()
        {
            var results = new List<Result>();

            results.Add(new Result
            {
                Title = "🏠 Sonarr Homepage",
                SubTitle = "Official Sonarr website",
                IcoPath = "Images\\icon.png",
                Score = 100,
                Action = _ => OpenUrl("https://sonarr.tv/")
            });

            results.Add(new Result
            {
                Title = "📚 Sonarr Wiki",
                SubTitle = "Complete documentation and guides",
                IcoPath = "Images\\icon.png",
                Score = 95,
                Action = _ => OpenUrl("https://wiki.servarr.com/sonarr")
            });

            results.Add(new Result
            {
                Title = "💬 Sonarr Discord",
                SubTitle = "Official Discord server for support",
                IcoPath = "Images\\icon.png",
                Score = 94,
                Action = _ => OpenUrl("https://discord.gg/M6BvZn5")
            });

            results.Add(new Result
            {
                Title = "📂 Sonarr GitHub",
                SubTitle = "Source code and issue tracking",
                IcoPath = "Images\\icon.png",
                Score = 93,
                Action = _ => OpenUrl("https://github.com/Sonarr/Sonarr")
            });

            return results;
        }

        private List<Result> GetGithubResults()
        {
            var results = new List<Result>();

            results.Add(new Result
            {
                Title = "💻 Plugin Repository",
                SubTitle = "Source code, issues, and contributions",
                IcoPath = "Images\\icon.png",
                Score = 100,
                Action = _ => OpenUrl("https://github.com/AtaraxyState/Sonarr-Explorer")
            });

            results.Add(new Result
            {
                Title = "🐛 Report Issue",
                SubTitle = "Report bugs or request features",
                IcoPath = "Images\\icon.png",
                Score = 95,
                Action = _ => OpenUrl("https://github.com/AtaraxyState/Sonarr-Explorer/issues/new")
            });

            results.Add(new Result
            {
                Title = "📋 View Issues",
                SubTitle = "Browse existing issues and discussions",
                IcoPath = "Images\\icon.png",
                Score = 94,
                Action = _ => OpenUrl("https://github.com/AtaraxyState/Sonarr-Explorer/issues")
            });

            results.Add(new Result
            {
                Title = "📈 Releases",
                SubTitle = "View plugin versions and changelog",
                IcoPath = "Images\\icon.png",
                Score = 93,
                Action = _ => OpenUrl("https://github.com/AtaraxyState/Sonarr-Explorer/releases")
            });

            return results;
        }

        private bool OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
} 