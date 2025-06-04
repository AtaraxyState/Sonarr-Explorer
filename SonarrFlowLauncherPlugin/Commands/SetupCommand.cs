using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SonarrFlowLauncherPlugin.Commands
{
    public class SetupCommand : BaseCommand
    {
        private readonly PluginInitContext? _context;

        public SetupCommand(SonarrService sonarrService, Settings settings, PluginInitContext? context = null) 
            : base(sonarrService, settings)
        {
            _context = context;
        }

        public override string CommandFlag => "-setup";
        public override string CommandName => "Setup Sonarr Plugin";
        public override string CommandDescription => "Guided setup for Sonarr API key and server configuration";

        public override List<Result> Execute(Query query)
        {
            var results = new List<Result>();
            
            // Parse the query to see what step we're on
            var setupQuery = query.Search
                .Replace(CommandFlag, "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (string.IsNullOrWhiteSpace(setupQuery))
            {
                return GetSetupStart();
            }
            else if (setupQuery.StartsWith("apikey ", StringComparison.OrdinalIgnoreCase))
            {
                return HandleApiKeyInput(setupQuery);
            }
            else if (setupQuery.StartsWith("server ", StringComparison.OrdinalIgnoreCase))
            {
                return HandleServerInput(setupQuery);
            }
            else if (setupQuery.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                return HandleHttpsToggle(true);
            }
            else if (setupQuery.Equals("http", StringComparison.OrdinalIgnoreCase))
            {
                return HandleHttpsToggle(false);
            }
            else
            {
                return GetSetupHelp();
            }
        }

        private List<Result> GetSetupStart()
        {
            var results = new List<Result>
            {
                new Result
                {
                    Title = "🚀 Sonarr Plugin Setup Wizard",
                    SubTitle = "Let's configure your Sonarr connection step by step",
                    IcoPath = "Images\\icon.png",
                    Score = 100
                }
            };

            if (string.IsNullOrWhiteSpace(Settings.ApiKey))
            {
                results.Add(new Result
                {
                    Title = "🔑 Step 1: Enter API Key",
                    SubTitle = "Type: snr -setup apikey YOUR_API_KEY_HERE",
                    IcoPath = "Images\\icon.png",
                    Score = 95,
                    Action = _ =>
                    {
                        _context?.API.ChangeQuery("snr -setup apikey ");
                        return false;
                    }
                });
            }
            else
            {
                results.Add(new Result
                {
                    Title = "✅ API Key Set",
                    SubTitle = $"API Key: {Settings.ApiKey.Substring(0, Math.Min(8, Settings.ApiKey.Length))}...",
                    IcoPath = "Images\\icon.png",
                    Score = 95
                });
            }

            if (string.IsNullOrWhiteSpace(Settings.ServerUrl) || Settings.ServerUrl == "localhost:8989")
            {
                results.Add(new Result
                {
                    Title = "🌐 Step 2: Enter Server URL",
                    SubTitle = "Type: snr -setup server YOUR_SERVER:PORT (e.g., localhost:8989)",
                    IcoPath = "Images\\icon.png",
                    Score = 90,
                    Action = _ =>
                    {
                        _context?.API.ChangeQuery("snr -setup server ");
                        return false;
                    }
                });
            }
            else
            {
                results.Add(new Result
                {
                    Title = "✅ Server URL Set",
                    SubTitle = $"Server: {(Settings.UseHttps ? "https" : "http")}://{Settings.ServerUrl}",
                    IcoPath = "Images\\icon.png",
                    Score = 90
                });
            }

            results.Add(new Result
            {
                Title = "🔒 Step 3: Protocol (Optional)",
                SubTitle = $"Currently: {(Settings.UseHttps ? "HTTPS" : "HTTP")} - Type 'snr -setup https' or 'snr -setup http'",
                IcoPath = "Images\\icon.png",
                Score = 85,
                Action = _ =>
                {
                    _context?.API.ChangeQuery($"snr -setup {(Settings.UseHttps ? "http" : "https")}");
                    return false;
                }
            });

            if (!string.IsNullOrWhiteSpace(Settings.ApiKey) && !string.IsNullOrWhiteSpace(Settings.ServerUrl))
            {
                results.Add(new Result
                {
                    Title = "🧪 Test Connection",
                    SubTitle = "Click to test your Sonarr connection",
                    IcoPath = "Images\\icon.png",
                    Score = 80,
                    Action = _ => 
                    {
                        // Start async test without blocking UI
                        Task.Run(async () => await TestConnectionAsync());
                        return true;
                    }
                });
            }

            results.Add(new Result
            {
                Title = "❓ Need Help Finding Your API Key?",
                SubTitle = "In Sonarr: Settings → General → API Key (copy the long string)",
                IcoPath = "Images\\icon.png",
                Score = 75
            });

            return results;
        }

        private List<Result> HandleApiKeyInput(string input)
        {
            var apiKey = input.Substring("apikey ".Length).Trim();
            
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "🔑 Enter Your API Key",
                        SubTitle = "Continue typing your Sonarr API key after 'apikey '",
                        IcoPath = "Images\\icon.png",
                        Score = 100
                    }
                };
            }

            if (apiKey.Length < 32)
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "⚠️ API Key Seems Too Short",
                        SubTitle = $"Current: {apiKey} (Sonarr API keys are usually 32+ characters)",
                        IcoPath = "Images\\icon.png",
                        Score = 100
                    },
                    new Result
                    {
                        Title = "💾 Save Anyway",
                        SubTitle = "Click to save this API key and continue",
                        IcoPath = "Images\\icon.png",
                        Score = 95,
                        Action = _ => SaveApiKey(apiKey)
                    }
                };
            }

            return new List<Result>
            {
                new Result
                {
                    Title = "✅ API Key Looks Good!",
                    SubTitle = $"Click to save: {apiKey.Substring(0, Math.Min(16, apiKey.Length))}...",
                    IcoPath = "Images\\icon.png",
                    Score = 100,
                    Action = _ => SaveApiKey(apiKey)
                }
            };
        }

        private List<Result> HandleServerInput(string input)
        {
            var serverUrl = input.Substring("server ".Length).Trim();
            
            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "🌐 Enter Your Server URL",
                        SubTitle = "Continue typing your server URL (e.g., localhost:8989 or 192.168.1.100:8989)",
                        IcoPath = "Images\\icon.png",
                        Score = 100
                    }
                };
            }

            // Clean up the URL (remove protocol if provided)
            serverUrl = serverUrl.Replace("http://", "").Replace("https://", "");

            return new List<Result>
            {
                new Result
                {
                    Title = "✅ Server URL Ready",
                    SubTitle = $"Click to save: {serverUrl}",
                    IcoPath = "Images\\icon.png",
                    Score = 100,
                    Action = _ => SaveServerUrl(serverUrl)
                }
            };
        }

        private List<Result> HandleHttpsToggle(bool useHttps)
        {
            Settings.UseHttps = useHttps;
            Settings.Save();

            return new List<Result>
            {
                new Result
                {
                    Title = $"✅ Protocol Set to {(useHttps ? "HTTPS" : "HTTP")}",
                    SubTitle = $"Your Sonarr will be accessed via {(useHttps ? "HTTPS" : "HTTP")}",
                    IcoPath = "Images\\icon.png",
                    Score = 100,
                    Action = _ =>
                    {
                        _context?.API.ChangeQuery("snr -setup");
                        return false;
                    }
                }
            };
        }

        private bool SaveApiKey(string apiKey)
        {
            Settings.ApiKey = apiKey;
            Settings.Save();
            
            _context?.API.ChangeQuery("snr -setup");
            return true;
        }

        private bool SaveServerUrl(string serverUrl)
        {
            Settings.ServerUrl = serverUrl;
            Settings.Save();
            
            _context?.API.ChangeQuery("snr -setup");
            return true;
        }

        private async Task TestConnectionAsync()
        {
            try
            {
                var testService = new SonarrService(Settings);
                var series = await testService.SearchSeriesAsync("");
                
                // For now, just dispose the service - we'll enhance this later
                testService.Dispose();
            }
            catch (Exception)
            {
                // Connection failed - just silently fail for now
            }
        }

        private List<Result> GetSetupHelp()
        {
            return new List<Result>
            {
                new Result
                {
                    Title = "🔧 Setup Commands",
                    SubTitle = "Available setup commands",
                    IcoPath = "Images\\icon.png",
                    Score = 100
                },
                new Result
                {
                    Title = "snr -setup",
                    SubTitle = "Start or return to setup wizard",
                    IcoPath = "Images\\icon.png",
                    Score = 95
                },
                new Result
                {
                    Title = "snr -setup apikey YOUR_KEY",
                    SubTitle = "Set your Sonarr API key",
                    IcoPath = "Images\\icon.png",
                    Score = 90
                },
                new Result
                {
                    Title = "snr -setup server YOUR_SERVER:PORT",
                    SubTitle = "Set your Sonarr server URL",
                    IcoPath = "Images\\icon.png",
                    Score = 85
                },
                new Result
                {
                    Title = "snr -setup https / snr -setup http",
                    SubTitle = "Toggle between HTTPS and HTTP",
                    IcoPath = "Images\\icon.png",
                    Score = 80
                }
            };
        }
    }
} 