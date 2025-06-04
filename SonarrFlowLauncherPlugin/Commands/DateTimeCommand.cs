using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Services;
using System.Globalization;

namespace SonarrFlowLauncherPlugin.Commands
{
    public class DateTimeCommand : BaseCommand
    {
        public DateTimeCommand(SonarrService sonarrService, Settings settings) 
            : base(sonarrService, settings)
        {
        }

        public override string CommandFlag => "-date";
        public override string CommandName => "Date & Time";
        public override string CommandDescription => "Date/time utilities and timezone conversions";

        public override List<Result> Execute(Query query)
        {
            var results = new List<Result>();
            
            var searchQuery = query.Search
                .Replace(CommandFlag, "", StringComparison.OrdinalIgnoreCase)
                .Replace("-time", "", StringComparison.OrdinalIgnoreCase)
                .Trim()
                .ToLower();

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return GetCurrentDateTimeResults();
            }
            else if (searchQuery.StartsWith("time") || query.Search.StartsWith("-time", StringComparison.OrdinalIgnoreCase))
            {
                var timezone = searchQuery.Replace("time", "").Trim();
                return GetTimezoneResults(timezone);
            }
            else if (searchQuery.Contains("convert"))
            {
                return GetConversionResults();
            }
            else if (searchQuery.Contains("utc"))
            {
                return GetUtcResults();
            }
            else
            {
                return GetCurrentDateTimeResults();
            }
        }

        private List<Result> GetCurrentDateTimeResults()
        {
            var results = new List<Result>();
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;

            // Current local time
            results.Add(new Result
            {
                Title = "🕒 Current Local Time",
                SubTitle = $"{now:dddd, MMMM d, yyyy 'at' h:mm:ss tt} ({TimeZoneInfo.Local.DisplayName})",
                IcoPath = "Images\\icon.png",
                Score = 100,
                Action = _ =>
                {
                    System.Windows.Clipboard.SetText(now.ToString("yyyy-MM-dd HH:mm:ss"));
                    return true;
                }
            });

            // Current UTC time
            results.Add(new Result
            {
                Title = "🌍 Current UTC Time",
                SubTitle = $"{utcNow:dddd, MMMM d, yyyy 'at' H:mm:ss} UTC",
                IcoPath = "Images\\icon.png",
                Score = 99,
                Action = _ =>
                {
                    System.Windows.Clipboard.SetText(utcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    return true;
                }
            });

            // Unix timestamp
            var unixTimestamp = ((DateTimeOffset)now).ToUnixTimeSeconds();
            results.Add(new Result
            {
                Title = "⏱️ Unix Timestamp",
                SubTitle = $"{unixTimestamp} (seconds since 1970-01-01)",
                IcoPath = "Images\\icon.png",
                Score = 98,
                Action = _ =>
                {
                    System.Windows.Clipboard.SetText(unixTimestamp.ToString());
                    return true;
                }
            });

            // Week info
            var weekOfYear = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            results.Add(new Result
            {
                Title = "📅 Week Information",
                SubTitle = $"Week {weekOfYear} of {now.Year} | Day {now.DayOfYear} of year",
                IcoPath = "Images\\icon.png",
                Score = 97
            });

            // Date calculations
            results.Add(new Result
            {
                Title = "🔢 Quick Calculations",
                SubTitle = "snr -date convert - Date conversion tools",
                IcoPath = "Images\\icon.png",
                Score = 96
            });

            return results;
        }

        private List<Result> GetTimezoneResults(string timezone)
        {
            var results = new List<Result>();
            var now = DateTime.UtcNow;

            // Common timezones if no specific one requested
            if (string.IsNullOrWhiteSpace(timezone))
            {
                var commonTimezones = new[]
                {
                    ("EST", "Eastern Standard Time"),
                    ("PST", "Pacific Standard Time"),
                    ("GMT", "GMT Standard Time"),
                    ("JST", "Tokyo Standard Time"),
                    ("CET", "Central Europe Standard Time"),
                    ("AEST", "AUS Eastern Standard Time")
                };

                results.Add(new Result
                {
                    Title = "🌐 World Time Zones",
                    SubTitle = "Current time in major timezones",
                    IcoPath = "Images\\icon.png",
                    Score = 100
                });

                foreach (var (abbr, tzId) in commonTimezones)
                {
                    try
                    {
                        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
                        var localTime = TimeZoneInfo.ConvertTimeFromUtc(now, tz);
                        
                        results.Add(new Result
                        {
                            Title = $"🕐 {abbr} - {tz.DisplayName}",
                            SubTitle = $"{localTime:HH:mm:ss} ({localTime:dddd, MMM d})",
                            IcoPath = "Images\\icon.png",
                            Score = 95,
                            Action = _ =>
                            {
                                System.Windows.Clipboard.SetText(localTime.ToString("yyyy-MM-dd HH:mm:ss"));
                                return true;
                            }
                        });
                    }
                    catch
                    {
                        // Skip invalid timezones
                    }
                }
            }

            return results;
        }

        private List<Result> GetConversionResults()
        {
            var results = new List<Result>();

            results.Add(new Result
            {
                Title = "🔄 Date Conversion Tools",
                SubTitle = "Various date and time conversion utilities",
                IcoPath = "Images\\icon.png",
                Score = 100
            });

            // Relative dates
            var now = DateTime.Now;
            var dates = new[]
            {
                ("Yesterday", now.AddDays(-1)),
                ("Tomorrow", now.AddDays(1)),
                ("Last Week", now.AddDays(-7)),
                ("Next Week", now.AddDays(7)),
                ("Last Month", now.AddMonths(-1)),
                ("Next Month", now.AddMonths(1))
            };

            foreach (var (name, date) in dates)
            {
                results.Add(new Result
                {
                    Title = $"📆 {name}",
                    SubTitle = $"{date:dddd, MMMM d, yyyy} ({Math.Abs((date - now).Days)} days)",
                    IcoPath = "Images\\icon.png",
                    Score = 95,
                    Action = _ =>
                    {
                        System.Windows.Clipboard.SetText(date.ToString("yyyy-MM-dd"));
                        return true;
                    }
                });
            }

            return results;
        }

        private List<Result> GetUtcResults()
        {
            var results = new List<Result>();
            var utcNow = DateTime.UtcNow;

            results.Add(new Result
            {
                Title = "🌍 UTC Time Information",
                SubTitle = "Coordinated Universal Time details",
                IcoPath = "Images\\icon.png",
                Score = 100
            });

            // Current UTC
            results.Add(new Result
            {
                Title = "⏰ Current UTC",
                SubTitle = $"{utcNow:yyyy-MM-dd HH:mm:ss} UTC",
                IcoPath = "Images\\icon.png",
                Score = 99,
                Action = _ =>
                {
                    System.Windows.Clipboard.SetText(utcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    return true;
                }
            });

            // ISO 8601 format
            results.Add(new Result
            {
                Title = "📄 ISO 8601 Format",
                SubTitle = $"{utcNow:yyyy-MM-ddTHH:mm:ss.fffZ}",
                IcoPath = "Images\\icon.png",
                Score = 98,
                Action = _ =>
                {
                    System.Windows.Clipboard.SetText(utcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                    return true;
                }
            });

            return results;
        }
    }
} 