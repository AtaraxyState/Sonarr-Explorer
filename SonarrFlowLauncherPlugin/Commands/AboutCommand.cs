using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Services;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.IO;

namespace SonarrFlowLauncherPlugin.Commands
{
    public class AboutCommand : BaseCommand
    {
        public AboutCommand(SonarrService sonarrService, Settings settings) 
            : base(sonarrService, settings)
        {
        }

        public override string CommandFlag => "-about";
        public override string CommandName => "About Plugin";
        public override string CommandDescription => "Show plugin information, version, and links";

        public override List<Result> Execute(Query query)
        {
            var results = new List<Result>();
            var pluginInfo = GetPluginInfo();

            // Plugin info
            results.Add(new Result
            {
                Title = "🚀 Sonarr Flow Launcher Plugin",
                SubTitle = $"Version {pluginInfo.Version} by {pluginInfo.Author}",
                IcoPath = "Images\\icon.png",
                Score = 100
            });

            // GitHub repository
            results.Add(new Result
            {
                Title = "📂 GitHub Repository",
                SubTitle = "View source code, report issues, contribute",
                IcoPath = "Images\\icon.png",
                Score = 95,
                Action = _ =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/AtaraxyState/Sonarr-Explorer",
                        UseShellExecute = true
                    });
                    return true;
                }
            });

            // Documentation
            results.Add(new Result
            {
                Title = "📖 Documentation",
                SubTitle = "Plugin setup guide and feature documentation",
                IcoPath = "Images\\icon.png",
                Score = 94,
                Action = _ =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/AtaraxyState/Sonarr-Explorer/blob/main/README.md",
                        UseShellExecute = true
                    });
                    return true;
                }
            });

            // Flow Launcher Plugin Store
            results.Add(new Result
            {
                Title = "🏪 Flow Launcher Plugin Store",
                SubTitle = "Find more plugins for Flow Launcher",
                IcoPath = "Images\\icon.png",
                Score = 93,
                Action = _ =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/Flow-Launcher/Flow.Launcher.Plugins",
                        UseShellExecute = true
                    });
                    return true;
                }
            });

            // Configuration status
            var configStatus = !string.IsNullOrEmpty(Settings.ApiKey) ? "✅ Configured" : "⚠️ Not Configured";
            var serverInfo = !string.IsNullOrEmpty(Settings.ServerUrl) ? Settings.ServerUrl : "Not set";
            
            results.Add(new Result
            {
                Title = $"⚙️ Configuration Status: {configStatus}",
                SubTitle = $"Server: {serverInfo} | HTTPS: {(Settings.UseHttps ? "Yes" : "No")}",
                IcoPath = "Images\\icon.png",
                Score = 92
            });

            // System info
            results.Add(new Result
            {
                Title = "💻 System Information",
                SubTitle = $".NET {Environment.Version} | {Environment.OSVersion}",
                IcoPath = "Images\\icon.png",
                Score = 91
            });

            // License
            results.Add(new Result
            {
                Title = "📄 License",
                SubTitle = "MIT License - Free and open source",
                IcoPath = "Images\\icon.png",
                Score = 90,
                Action = _ =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/AtaraxyState/Sonarr-Explorer/blob/main/LICENSE",
                        UseShellExecute = true
                    });
                    return true;
                }
            });

            return results;
        }

        private (string Version, string Author) GetPluginInfo()
        {
            try
            {
                var currentAssembly = Assembly.GetExecutingAssembly();
                var pluginDir = Path.GetDirectoryName(currentAssembly.Location);
                var pluginJsonPath = Path.Combine(pluginDir!, "plugin.json");

                if (File.Exists(pluginJsonPath))
                {
                    var json = File.ReadAllText(pluginJsonPath);
                    var pluginData = JObject.Parse(json);
                    
                    var pluginVersion = pluginData["Version"]?.ToString() ?? "Unknown";
                    var pluginAuthor = pluginData["Author"]?.ToString() ?? "Rain";
                    
                    return (pluginVersion, pluginAuthor);
                }
            }
            catch
            {
                // Fall back to assembly version if plugin.json reading fails
            }

            // Fallback to assembly version
            var fallbackAssembly = Assembly.GetExecutingAssembly();
            var fallbackVersion = fallbackAssembly.GetName().Version?.ToString() ?? "Unknown";
            return (fallbackVersion, "Rain");
        }
    }
} 