using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Models;
using SonarrFlowLauncherPlugin.Services;

namespace SonarrFlowLauncherPlugin.Commands
{
    /// <summary>
    /// Command for managing Sonarr system health checks and status monitoring.
    /// Provides functionality to view health issues, trigger re-tests, and access system status.
    /// </summary>
    public class SystemCommand : BaseCommand
    {
        /// <summary>
        /// Initializes a new instance of the SystemCommand.
        /// </summary>
        /// <param name="sonarrService">Service for Sonarr API communication</param>
        /// <param name="settings">Plugin settings instance</param>
        public SystemCommand(SonarrService sonarrService, Settings settings)
            : base(sonarrService, settings)
        {
        }

        /// <summary>
        /// Gets the command flag used to trigger this command.
        /// </summary>
        public override string CommandFlag => "-s";

        /// <summary>
        /// Gets the human-readable name of this command.
        /// </summary>
        public override string CommandName => "System Health";

        /// <summary>
        /// Gets a detailed description of what this command does.
        /// </summary>
        public override string CommandDescription => "Monitor Sonarr system health, view issues, and trigger health check re-tests";

        /// <summary>
        /// Executes the system health command logic.
        /// </summary>
        /// <param name="query">User input query containing command parameters</param>
        /// <returns>List of results showing health status and issues</returns>
        public override List<Result> Execute(Query query)
        {
            if (!ValidateSettings())
            {
                return GetSettingsError();
            }

            var results = new List<Result>();

            try
            {
                // Add the "Test All" button first
                results.Add(CreateTestAllButton());

                // Fetch and display health checks
                var healthChecks = Task.Run(async () => await SonarrService.GetHealthChecksAsync()).Result;

                if (healthChecks.Count == 0)
                {
                    results.Add(new Result
                    {
                        Title = "✅ All Systems Healthy",
                        SubTitle = "No health check issues found - All systems operating normally",
                        IcoPath = "Images\\icon.png",
                        Score = 95,
                        Action = _ => false
                    });
                }
                else
                {
                    // Add header for health issues
                    results.Add(new Result
                    {
                        Title = $"⚠️ Found {healthChecks.Count} Health Issue{(healthChecks.Count == 1 ? "" : "s")}",
                        SubTitle = "Click on any issue to re-test it | Right-click for more options",
                        IcoPath = "Images\\icon.png",
                        Score = 90,
                        Action = _ => false
                    });

                    // Add each health check as a result
                    foreach (var healthCheck in healthChecks.OrderByDescending(h => h.Type == "error" ? 2 : h.Type == "warning" ? 1 : 0))
                    {
                        results.Add(CreateHealthCheckResult(healthCheck));
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add(new Result
                {
                    Title = "❌ Error Fetching Health Status",
                    SubTitle = $"Failed to retrieve health checks: {ex.Message}",
                    IcoPath = "Images\\icon.png",
                    Score = 100,
                    Action = _ => false
                });
            }

            return results;
        }

        /// <summary>
        /// Creates the "Test All" button result.
        /// </summary>
        /// <returns>Result for testing all health checks</returns>
        private Result CreateTestAllButton()
        {
            return new Result
            {
                Title = "🔄 Test All Health Checks",
                SubTitle = "Trigger a complete health check re-test for all systems",
                IcoPath = "Images\\icon.png",
                Score = 100,
                Action = _ =>
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var success = await SonarrService.TriggerHealthCheckAsync();
                            System.Diagnostics.Debug.WriteLine(success 
                                ? "Health check re-test triggered successfully" 
                                : "Failed to trigger health check re-test");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error triggering health check: {ex.Message}");
                        }
                    });
                    return true;
                }
            };
        }

        /// <summary>
        /// Creates a result item for a specific health check.
        /// </summary>
        /// <param name="healthCheck">The health check to create a result for</param>
        /// <returns>Result with health check information and actions</returns>
        private Result CreateHealthCheckResult(SonarrHealthCheck healthCheck)
        {
            return new Result
            {
                Title = healthCheck.GetDisplayTitle(),
                SubTitle = healthCheck.GetDisplaySubTitle(),
                IcoPath = "Images\\icon.png",
                Score = healthCheck.Type.ToLower() == "error" ? 85 : 80,
                Action = _ =>
                {
                    // Left click action: Re-test this specific health check
                    Task.Run(async () =>
                    {
                        try
                        {
                            var success = await SonarrService.RetestHealthCheckAsync(healthCheck);
                            System.Diagnostics.Debug.WriteLine(success 
                                ? $"Health check re-test triggered for: {healthCheck.Source}" 
                                : $"Failed to trigger re-test for: {healthCheck.Source}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error retesting health check {healthCheck.Source}: {ex.Message}");
                        }
                    });
                    return true;
                },
                ContextData = healthCheck // Store health check for context menu
            };
        }

        /// <summary>
        /// Creates context menu items for health check results.
        /// This would be called by the ContextMenuService if integrated.
        /// </summary>
        /// <param name="healthCheck">The health check to create context menu for</param>
        /// <returns>List of context menu results</returns>
        public static List<Result> CreateHealthCheckContextMenu(SonarrHealthCheck healthCheck, SonarrService sonarrService)
        {
            var contextMenu = new List<Result>();

            // Open in Sonarr option
            contextMenu.Add(new Result
            {
                Title = "🌐 Open in Sonarr",
                SubTitle = "View this issue in Sonarr's System → Status page",
                IcoPath = "Images\\icon.png",
                Score = 100,
                Action = _ =>
                {
                    sonarrService.OpenSystemStatusInBrowser();
                    return true;
                }
            });

            // Re-test option
            contextMenu.Add(new Result
            {
                Title = "🔄 Re-test This Issue",
                SubTitle = $"Trigger a re-test for: {healthCheck.Source}",
                IcoPath = "Images\\icon.png",
                Score = 95,
                Action = _ =>
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var success = await sonarrService.RetestHealthCheckAsync(healthCheck);
                            System.Diagnostics.Debug.WriteLine(success 
                                ? $"Health check re-test triggered for: {healthCheck.Source}" 
                                : $"Failed to trigger re-test for: {healthCheck.Source}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error retesting health check {healthCheck.Source}: {ex.Message}");
                        }
                    });
                    return true;
                }
            });

            // Copy error message option
            contextMenu.Add(new Result
            {
                Title = "📋 Copy Error Message",
                SubTitle = $"Copy to clipboard: {healthCheck.Message}",
                IcoPath = "Images\\icon.png",
                Score = 90,
                Action = _ =>
                {
                    try
                    {
                        System.Windows.Clipboard.SetText($"{healthCheck.Source}: {healthCheck.Message}");
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            });

            // Help/Wiki option (if available)
            if (!string.IsNullOrEmpty(healthCheck.WikiUrl))
            {
                contextMenu.Add(new Result
                {
                    Title = "❓ View Help Documentation",
                    SubTitle = $"Open help documentation for this issue",
                    IcoPath = "Images\\icon.png",
                    Score = 85,
                    Action = _ =>
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = healthCheck.WikiUrl,
                                UseShellExecute = true
                            });
                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                });
            }

            return contextMenu;
        }
    }
} 