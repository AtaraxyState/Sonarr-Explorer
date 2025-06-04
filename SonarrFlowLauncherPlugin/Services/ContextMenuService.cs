using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Models;

namespace SonarrFlowLauncherPlugin.Services
{
    public class ContextMenuService
    {
        private readonly SonarrService _sonarrService;

        public ContextMenuService(SonarrService sonarrService)
        {
            _sonarrService = sonarrService;
        }

        public List<Result> BuildSeriesContextMenu(SonarrSeries series)
        {
            var contextMenus = new List<Result>();

            // Always add refresh option as the first item
            contextMenus.Add(CreateRefreshSeriesResult(series.Id, series.Title, 1000));

            // Add option to open series in browser
            contextMenus.Add(CreateOpenInSonarrResult(series.TitleSlug, 999));

            // Scan for video files and add folder/file options
            if (!string.IsNullOrEmpty(series.Path) && Directory.Exists(series.Path))
            {
                contextMenus.Add(CreateOpenFolderResult(series.Path, 998));
                AddVideoFileResults(contextMenus, series.Path, null, 997);
            }
            else
            {
                AddPathNotFoundResults(contextMenus, series.Path, series.TitleSlug);
            }

            return contextMenus;
        }

        public List<Result> BuildEpisodeContextMenu(SonarrEpisodeBase episodeItem)
        {
            var contextMenus = new List<Result>();
            var seriesTitle = !string.IsNullOrEmpty(episodeItem.SeriesTitle) ? episodeItem.SeriesTitle : episodeItem.Title;

            // Add refresh option as the first item
            if (episodeItem.SeriesId > 0)
            {
                contextMenus.Add(CreateRefreshSeriesResult(episodeItem.SeriesId, seriesTitle, 1000));
            }

            // Add option to open series in Sonarr web UI
            if (!string.IsNullOrEmpty(episodeItem.TitleSlug))
            {
                contextMenus.Add(CreateOpenInSonarrResult(episodeItem.TitleSlug, 999));
            }

            // Scan for specific episode file or show folder options
            var episodeFileFound = false;
            if (!string.IsNullOrEmpty(episodeItem.SeriesPath) && Directory.Exists(episodeItem.SeriesPath))
            {
                contextMenus.Add(CreateOpenFolderResult(episodeItem.SeriesPath, 998));
                
                var episodePattern = $"S{episodeItem.SeasonNumber:D2}E{episodeItem.EpisodeNumber:D2}";
                episodeFileFound = AddVideoFileResults(contextMenus, episodeItem.SeriesPath, episodePattern, 997);
            }

            // Fallback: Add option to open episode file if path is already known
            if (!episodeFileFound && !string.IsNullOrEmpty(episodeItem.EpisodeFilePath) && File.Exists(episodeItem.EpisodeFilePath))
            {
                contextMenus.Add(CreateOpenFileResult(episodeItem.EpisodeFilePath, 997));
            }

            // Add episode-specific details
            AddEpisodeSpecificResults(contextMenus, episodeItem);

            return contextMenus;
        }

        private Result CreateRefreshSeriesResult(int seriesId, string seriesTitle, int score)
        {
            return new Result
            {
                Title = "🔄 Refresh Series",
                SubTitle = $"Trigger a rescan of {seriesTitle}",
                IcoPath = "Images\\icon.png",
                Score = score,
                Action = _ => RefreshSeries(seriesId)
            };
        }

        private Result CreateOpenInSonarrResult(string titleSlug, int score)
        {
            return new Result
            {
                Title = "🌐 Open Series in Sonarr",
                SubTitle = "Open series page in Sonarr web interface",
                IcoPath = "Images\\icon.png",
                Score = score,
                Action = _ => _sonarrService.OpenSeriesInBrowser(titleSlug)
            };
        }

        private Result CreateOpenFolderResult(string path, int score)
        {
            return new Result
            {
                Title = "📁 Open Series Folder",
                SubTitle = $"Open {path} in Windows Explorer",
                IcoPath = "Images\\icon.png",
                Score = score,
                Action = _ => OpenFolderInExplorer(path)
            };
        }

        private Result CreateOpenFileResult(string filePath, int score)
        {
            var fileName = Path.GetFileName(filePath);
            var fileInfo = new FileInfo(filePath);
            var fileSize = FormatFileSize(fileInfo.Length);
            var resolution = ExtractResolution(fileName);
            var resolutionText = !string.IsNullOrEmpty(resolution) ? $" | {resolution}" : "";

            return new Result
            {
                Title = $"▶️ {fileName}",
                SubTitle = $"Open episode file | {fileSize}{resolutionText}",
                IcoPath = filePath,
                Score = score,
                Action = _ => OpenFileWithDefaultApp(filePath)
            };
        }

        private bool AddVideoFileResults(List<Result> contextMenus, string seriesPath, string episodePattern, int startScore)
        {
            try
            {
                var videoExtensions = new[] { ".mkv", ".mp4", ".avi", ".m4v", ".mov", ".wmv", ".mpg", ".mpeg", ".ts", ".flv" };
                var videoFiles = Directory.GetFiles(seriesPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => videoExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .OrderBy(f => f)
                    .ToList();

                Debug.WriteLine($"Found {videoFiles.Count} video files in {seriesPath}");

                List<string> matchingFiles;
                if (!string.IsNullOrEmpty(episodePattern))
                {
                    // Filter for specific episode pattern
                    matchingFiles = videoFiles.Where(f => 
                        Path.GetFileName(f).Contains(episodePattern, StringComparison.OrdinalIgnoreCase))
                        .Take(3) // Limit to first 3 matches for episodes
                        .ToList();
                }
                else
                {
                    // Show all video files for series
                    matchingFiles = videoFiles;
                }

                if (matchingFiles.Any())
                {
                    var score = startScore;
                    foreach (var videoFile in matchingFiles)
                    {
                        var fileName = Path.GetFileName(videoFile);
                        var fileInfo = new FileInfo(videoFile);
                        var fileSize = FormatFileSize(fileInfo.Length);
                        
                        string displayName;
                        if (string.IsNullOrEmpty(episodePattern))
                        {
                            // For series context menu, extract episode info from filename
                            var episodePatternMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"[Ss](\d+)[Ee](\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            displayName = episodePatternMatch.Success ? 
                                $"S{episodePatternMatch.Groups[1].Value.PadLeft(2, '0')}E{episodePatternMatch.Groups[2].Value.PadLeft(2, '0')} - {fileName}" : 
                                fileName;
                        }
                        else
                        {
                            // For episode context menu, show just the filename
                            displayName = fileName;
                        }

                        var resolution = ExtractResolution(fileName);
                        var resolutionText = !string.IsNullOrEmpty(resolution) ? $" | {resolution}" : "";

                        contextMenus.Add(new Result
                        {
                            Title = $"▶️ {displayName}",
                            SubTitle = $"Open with default player | {fileSize}{resolutionText}",
                            IcoPath = videoFile,
                            Score = score--,
                            Action = _ => OpenFileWithDefaultApp(videoFile)
                        });
                    }
                    return true;
                }
                else if (string.IsNullOrEmpty(episodePattern))
                {
                    // Only show "no files found" for series context menu
                    contextMenus.Add(new Result
                    {
                        Title = "📁 No Video Files Found",
                        SubTitle = $"No video files found in {seriesPath}",
                        IcoPath = "Images\\icon.png",
                        Score = startScore,
                        Action = _ => OpenFolderInExplorer(seriesPath)
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error scanning for video files: {ex.Message}");
                contextMenus.Add(new Result
                {
                    Title = "❌ Error Scanning Files",
                    SubTitle = $"Could not scan folder: {ex.Message}",
                    IcoPath = "Images\\icon.png",
                    Score = startScore,
                    Action = _ => false
                });
            }

            return false;
        }

        private void AddPathNotFoundResults(List<Result> contextMenus, string seriesPath, string titleSlug)
        {
            Debug.WriteLine($"Series path issue - Path: '{seriesPath}', IsEmpty: {string.IsNullOrEmpty(seriesPath)}, Exists: {(!string.IsNullOrEmpty(seriesPath) ? Directory.Exists(seriesPath).ToString() : "N/A")}");
            
            var pathInfo = string.IsNullOrEmpty(seriesPath) ? 
                "No path configured in Sonarr" : 
                $"Path does not exist: {seriesPath}";
            
            contextMenus.Add(new Result
            {
                Title = "⚠️ Series Path Not Found",
                SubTitle = pathInfo,
                IcoPath = "Images\\icon.png",
                Score = 998,
                Action = _ => false
            });

            if (!string.IsNullOrEmpty(seriesPath))
            {
                contextMenus.Add(new Result
                {
                    Title = "💡 Path Debugging Info",
                    SubTitle = $"Configured path: {seriesPath} | Click to copy",
                    IcoPath = "Images\\icon.png",
                    Score = 997,
                    Action = _ => CopyToClipboard(seriesPath)
                });
            }

            contextMenus.Add(new Result
            {
                Title = "🔧 Check Sonarr Settings",
                SubTitle = "Verify series path in Sonarr → Series → Edit → Path",
                IcoPath = "Images\\icon.png",
                Score = 996,
                Action = _ => _sonarrService.OpenSeriesInBrowser(titleSlug)
            });
        }

        private void AddEpisodeSpecificResults(List<Result> contextMenus, SonarrEpisodeBase episodeItem)
        {
            if (episodeItem is SonarrQueueItem queueItem)
            {
                contextMenus.Add(new Result
                {
                    Title = "📊 Download Progress",
                    SubTitle = $"Progress: {queueItem.Progress:F1}% - Status: {queueItem.Status}",
                    IcoPath = "Images\\icon.png",
                    Score = 80,
                    Action = _ => false
                });
            }
            else if (episodeItem is SonarrHistoryItem historyItem)
            {
                contextMenus.Add(new Result
                {
                    Title = "📅 Event Details",
                    SubTitle = $"Event: {historyItem.EventType} - Date: {historyItem.Date:g}",
                    IcoPath = "Images\\icon.png",
                    Score = 80,
                    Action = _ => false
                });
            }
            else if (episodeItem is SonarrCalendarItem calendarItem)
            {
                contextMenus.Add(new Result
                {
                    Title = "📅 Episode Details",
                    SubTitle = $"Air Date: {calendarItem.AirDate:g} | {(calendarItem.HasFile ? "Downloaded" : "Not Downloaded")} | {(calendarItem.Monitored ? "Monitored" : "Not Monitored")}",
                    IcoPath = "Images\\icon.png",
                    Score = 80,
                    Action = _ => false
                });
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private string ExtractResolution(string fileName)
        {
            var resolutionPatterns = new[]
            {
                @"\b2160p\b|4K|UHD",        // 4K/2160p
                @"\b1080p\b",               // 1080p
                @"\b720p\b",                // 720p
                @"\b480p\b",                // 480p
                @"\b360p\b"                 // 360p
            };

            foreach (var pattern in resolutionPatterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(fileName, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var resolution = match.Value.ToUpper();
                    if (resolution.Contains("2160") || resolution.Contains("4K") || resolution.Contains("UHD"))
                        return "4K";
                    return resolution;
                }
            }

            return string.Empty;
        }

        private bool RefreshSeries(int seriesId)
        {
            try
            {
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        await _sonarrService.RefreshSeriesAsync(seriesId);
                        Debug.WriteLine($"Successfully refreshed series: {seriesId}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to refresh series {seriesId}: {ex.Message}");
                    }
                });
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting refresh for series {seriesId}: {ex.Message}");
                return false;
            }
        }

        private bool OpenFolderInExplorer(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{path}\"",
                        UseShellExecute = true
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening folder: {ex.Message}");
            }
            return false;
        }

        private bool OpenFileWithDefaultApp(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening file: {ex.Message}");
            }
            return false;
        }

        private bool CopyToClipboard(string text)
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error copying to clipboard: {ex.Message}");
                return false;
            }
        }
    }
} 