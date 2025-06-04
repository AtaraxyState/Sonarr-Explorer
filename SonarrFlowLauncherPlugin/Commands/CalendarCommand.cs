using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SonarrFlowLauncherPlugin.Commands
{
    public class CalendarCommand : BaseCommand
    {
        public CalendarCommand(SonarrService sonarrService, Settings settings) 
            : base(sonarrService, settings)
        {
        }

        public override string CommandFlag => "-c";
        public override string CommandName => "View Sonarr Calendar";
        public override string CommandDescription => "View upcoming episodes (use: today, tomorrow, week)";

        public override List<Result> Execute(Query query)
        {
            if (!ValidateSettings())
            {
                return GetSettingsError();
            }

            var results = new List<Result>();
            // Clean up the query string: remove command flag, trim spaces, and convert to lowercase
            var searchQuery = query.Search
                .Replace(CommandFlag, "", StringComparison.OrdinalIgnoreCase)
                .Trim()
                .ToLower();

            System.Diagnostics.Debug.WriteLine($"Calendar command received. Raw query: '{query.Search}'");
            System.Diagnostics.Debug.WriteLine($"Cleaned search query: '{searchQuery}'");

            try
            {
                DateTime start = DateTime.Today;
                DateTime end = DateTime.Today.AddDays(7); // Default to a week

                // Parse date range based on query
                switch (searchQuery)
                {
                    case "today":
                        start = DateTime.Today;
                        end = DateTime.Today.AddDays(1).AddSeconds(-1); // End at 23:59:59 today
                        System.Diagnostics.Debug.WriteLine($"Today query - Start: {start:yyyy-MM-dd HH:mm:ss}, End: {end:yyyy-MM-dd HH:mm:ss}");
                        break;
                    case "tomorrow":
                        start = DateTime.Today.AddDays(1);
                        end = start.AddDays(1).AddSeconds(-1); // End at 23:59:59 tomorrow
                        System.Diagnostics.Debug.WriteLine($"Tomorrow query - Start: {start:yyyy-MM-dd HH:mm:ss}, End: {end:yyyy-MM-dd HH:mm:ss}");
                        break;
                    case "week":
                        // Default values are already set
                        System.Diagnostics.Debug.WriteLine($"Week query - Start: {start:yyyy-MM-dd HH:mm:ss}, End: {end:yyyy-MM-dd HH:mm:ss}");
                        break;
                    case "next week":
                        start = DateTime.Today.AddDays(7);
                        end = start.AddDays(7);
                        System.Diagnostics.Debug.WriteLine($"Next week query - Start: {start:yyyy-MM-dd HH:mm:ss}, End: {end:yyyy-MM-dd HH:mm:ss}");
                        break;
                    case "month":
                        end = DateTime.Today.AddMonths(1);
                        System.Diagnostics.Debug.WriteLine($"Month query - Start: {start:yyyy-MM-dd HH:mm:ss}, End: {end:yyyy-MM-dd HH:mm:ss}");
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine($"Default/unknown query '{searchQuery}' - Start: {start:yyyy-MM-dd HH:mm:ss}, End: {end:yyyy-MM-dd HH:mm:ss}");
                        break;
                }

                System.Diagnostics.Debug.WriteLine("Calling GetCalendarAsync...");
                var calendar = SonarrService.GetCalendarAsync(start, end).Result;
                System.Diagnostics.Debug.WriteLine($"Got {calendar.Count} calendar items");

                if (!calendar.Any())
                {
                    results.Add(new Result
                    {
                        Title = "No Episodes Found",
                        SubTitle = $"No episodes scheduled between {start:d} and {end:d}",
                        IcoPath = "Images\\icon.png",
                        Score = 100
                    });
                }
                else
                {
                    // Group episodes by date
                    var groupedEpisodes = calendar
                        .GroupBy(e => e.AirDate.Date)
                        .OrderBy(g => g.Key);

                    foreach (var group in groupedEpisodes)
                    {
                        var dateHeader = group.Key.Date == DateTime.Today
                            ? "Today"
                            : group.Key.Date == DateTime.Today.AddDays(1)
                                ? "Tomorrow"
                                : group.Key.ToString("dddd, MMM d");

                        // Add date header
                        results.Add(new Result
                        {
                            Title = $"📅 {dateHeader}",
                            SubTitle = $"{group.Count()} episode{(group.Count() != 1 ? "s" : "")}",
                            IcoPath = "Images\\icon.png",
                            Score = 100
                        });

                        // Add episodes for this date
                        var index = 0;
                        foreach (var episode in group.OrderBy(e => e.Title))
                        {
                            var status = episode.HasFile ? "✅" : episode.Monitored ? "⏰" : "⚪";
                            results.Add(new Result
                            {
                                Title = $"{(episode.HasFile ? "✅" : "📅")} {episode.SeriesTitle}",
                                SubTitle = $"S{episode.SeasonNumber:D2}E{episode.EpisodeNumber:D2} - {episode.EpisodeTitle} - {episode.AirDate:g}",
                                IcoPath = !string.IsNullOrEmpty(episode.PosterPath) ? episode.PosterPath : "Images\\icon.png",
                                Score = 100 - index,
                                ContextData = episode,
                                Action = _ =>
                                {
                                    if (!string.IsNullOrEmpty(episode.Overview))
                                    {
                                        MessageBox.Show(episode.Overview, $"{episode.SeriesTitle} - {episode.EpisodeTitle}", MessageBoxButton.OK, MessageBoxImage.Information);
                                    }
                                    return true;
                                }
                            });
                            index++;
                        }
                    }
                }

                // Add help result if no specific query
                if (string.IsNullOrEmpty(searchQuery))
                {
                    results.Insert(0, new Result
                    {
                        Title = "Calendar Options",
                        SubTitle = "Type: today, tomorrow, week, next week, month",
                        IcoPath = "Images\\icon.png",
                        Score = 100
                    });
                }

                // Add option to open in browser
                results.Add(new Result
                {
                    Title = "Open Calendar in Browser",
                    SubTitle = "View full calendar in Sonarr",
                    IcoPath = "Images\\icon.png",
                    Score = 80,
                    Action = _ => SonarrService.OpenCalendarInBrowser()
                });
            }
            catch (Exception ex)
            {
                results.Add(new Result
                {
                    Title = "Error Getting Calendar",
                    SubTitle = $"Error: {ex.Message}",
                    IcoPath = "Images\\icon.png",
                    Score = 100
                });
            }

            return results;
        }
    }
} 