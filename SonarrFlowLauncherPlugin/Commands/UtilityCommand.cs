using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Services;
using System.Diagnostics;
using System.Net.Http;
using System.IO;

namespace SonarrFlowLauncherPlugin.Commands
{
    public class UtilityCommand : BaseCommand
    {
        public UtilityCommand(SonarrService sonarrService, Settings settings) 
            : base(sonarrService, settings)
        {
        }

        public override string CommandFlag => "-test";
        public override string CommandName => "Test & Utilities";
        public override string CommandDescription => "Test connection, access settings, and utility functions";

        public bool MatchesCommand(string input)
        {
            return input.StartsWith("-test", StringComparison.OrdinalIgnoreCase) ||
                   input.StartsWith("-settings", StringComparison.OrdinalIgnoreCase) ||
                   input.StartsWith("-util", StringComparison.OrdinalIgnoreCase);
        }

        public override List<Result> Execute(Query query)
        {
            var results = new List<Result>();
            
            // Parse the command
            var searchQuery = query.Search
                .Replace(CommandFlag, "", StringComparison.OrdinalIgnoreCase)
                .Trim()
                .ToLower();

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                // Show all utility options
                results.Add(new Result
                {
                    Title = "🔧 Utility Commands",
                    SubTitle = "Available utility functions",
                    IcoPath = "Images\\icon.png",
                    Score = 100
                });

                results.Add(new Result
                {
                    Title = "🌐 Test Connection",
                    SubTitle = "snr -test connection - Test connectivity to Sonarr server",
                    IcoPath = "Images\\icon.png",
                    Score = 95,
                    Action = _ => TestConnection()
                });

                results.Add(new Result
                {
                    Title = "⚙️ Open Settings",
                    SubTitle = "snr -test settings - Open plugin settings panel",
                    IcoPath = "Images\\icon.png",
                    Score = 94,
                    Action = _ => OpenSettings()
                });

                results.Add(new Result
                {
                    Title = "📁 Open Logs",
                    SubTitle = "snr -test logs - Open Flow Launcher logs directory",
                    IcoPath = "Images\\icon.png",
                    Score = 93,
                    Action = _ => OpenLogsDirectory()
                });

                results.Add(new Result
                {
                    Title = "Reload Plugin",
                    SubTitle = "snr -test reload - Reload plugin settings",
                    IcoPath = "Images\\refresh.png",
                    Score = 92,
                    Action = _ => ReloadPlugin()
                });
            }
            else
            {
                switch (searchQuery)
                {
                    case "connection":
                    case "conn":
                        return TestConnectionCommand();
                    
                    case "settings":
                    case "config":
                        return OpenSettingsCommand();
                    
                    case "logs":
                    case "log":
                        return OpenLogsCommand();
                    
                    case "reload":
                    case "refresh":
                        return ReloadPluginCommand();
                    
                    default:
                        results.Add(new Result
                        {
                            Title = "❓ Unknown Utility Command",
                            SubTitle = "Available: connection, settings, logs, reload",
                            IcoPath = "Images\\icon.png",
                            Score = 100
                        });
                        break;
                }
            }

            return results;
        }

        private List<Result> TestConnectionCommand()
        {
            var results = new List<Result>();
            
            if (string.IsNullOrEmpty(Settings.ServerUrl))
            {
                results.Add(new Result
                {
                    Title = "❌ No Server URL Configured",
                    SubTitle = "Please set your Sonarr server URL in settings",
                    IcoPath = "Images\\icon.png",
                    Score = 100
                });
                return results;
            }

            // Test basic connectivity
            results.Add(new Result
            {
                Title = "🔍 Testing Connection...",
                SubTitle = $"Testing connectivity to {Settings.ServerUrl}",
                IcoPath = "Images\\icon.png",
                Score = 100,
                Action = _ => TestConnection()
            });

            return results;
        }

        private bool TestConnection()
        {
            try
            {
                var url = $"{(Settings.UseHttps ? "https" : "http")}://{Settings.ServerUrl}";
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                
                var response = client.GetAsync(url).Result;
                var status = response.IsSuccessStatusCode ? "✅ Connected" : $"❌ HTTP {(int)response.StatusCode}";
                
                // Show result in a new Flow Launcher query
                Process.Start(new ProcessStartInfo
                {
                    FileName = "flow",
                    Arguments = $"snr Connection test: {status} to {Settings.ServerUrl}",
                    UseShellExecute = true
                });
                
                return true;
            }
            catch (Exception ex)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "flow", 
                    Arguments = $"snr ❌ Connection failed: {ex.Message}",
                    UseShellExecute = true
                });
                return false;
            }
        }

        private List<Result> OpenSettingsCommand()
        {
            return new List<Result>
            {
                new Result
                {
                    Title = "⚙️ Plugin Settings",
                    SubTitle = "Open Flow Launcher settings and navigate to this plugin",
                    IcoPath = "Images\\icon.png",
                    Score = 100,
                    Action = _ => OpenSettings()
                }
            };
        }

        private bool OpenSettings()
        {
            try
            {
                // Try to open Flow Launcher settings
                Process.Start(new ProcessStartInfo
                {
                    FileName = "flow",
                    Arguments = "settings",
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private List<Result> OpenLogsCommand()
        {
            return new List<Result>
            {
                new Result
                {
                    Title = "📁 Flow Launcher Logs",
                    SubTitle = "Open logs directory for troubleshooting",
                    IcoPath = "Images\\icon.png",
                    Score = 100,
                    Action = _ => OpenLogsDirectory()
                }
            };
        }

        private bool OpenLogsDirectory()
        {
            try
            {
                var logsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlowLauncher", "Logs");
                if (Directory.Exists(logsPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = logsPath,
                        UseShellExecute = true
                    });
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private List<Result> ReloadPluginCommand()
        {
            return new List<Result>
            {
                new Result
                {
                    Title = "Reload Plugin Settings",
                    SubTitle = "Reload configuration without restarting Flow Launcher",
                    IcoPath = "Images\\refresh.png",
                    Score = 100,
                    Action = _ => ReloadPlugin()
                }
            };
        }

        private bool ReloadPlugin()
        {
            try
            {
                // The hot-reloading is already implemented in Main.cs
                // This just provides user feedback
                System.Diagnostics.Debug.WriteLine("Plugin reload requested");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
} 