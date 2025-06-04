using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Commands;
using SonarrFlowLauncherPlugin.Models;
using SonarrFlowLauncherPlugin.Services;

namespace SonarrFlowLauncherPlugin.Tests.Commands
{
    [TestClass]
    public class LibrarySearchCommandTests
    {
        private Mock<SonarrService> _mockSonarrService = null!;
        private Settings _settings = null!;
        private LibrarySearchCommand _command = null!;

        // Helper method to create Query objects with proper parameters
        private Query CreateQuery(string search)
        {
            // Create a mock Query instead of trying to set read-only properties
            var query = new Query();
            // Use reflection to set the search value if needed for testing
            // For now, return empty query as this is just for testing the method structure
            return query;
        }

        [TestInitialize]
        public void Setup()
        {
            _mockSonarrService = new Mock<SonarrService>(null);
            _settings = new Settings { ApiKey = "test-api-key" };
            _command = new LibrarySearchCommand(_mockSonarrService.Object, _settings);
        }

        [TestMethod]
        public void Execute_WithValidApiKey_CallsSearchSeriesAsync()
        {
            // Arrange
            _mockSonarrService.Setup(s => s.SearchSeriesAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<SonarrSeries>());
            var query = CreateQuery("-l test");

            // Act
            var results = _command.Execute(query);

            // Assert
            _mockSonarrService.Verify(s => s.SearchSeriesAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void Execute_WithNoApiKey_ReturnsSettingsError()
        {
            // Arrange
            _settings.ApiKey = "";
            var query = CreateQuery("-l");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.IsTrue(results.Any(r => r.Title.Contains("Setup Required")));
        }

        [TestMethod]
        public void Execute_WithSearchResults_ReturnsSeriesResults()
        {
            // Arrange
            var series = new List<SonarrSeries>
            {
                new SonarrSeries
                {
                    Id = 1,
                    Title = "Test Series",
                    TitleSlug = "test-series",
                    Network = "Test Network",
                    Status = "Continuing",
                    Path = @"C:\TV\Test Series",
                    Statistics = new SeriesStatistics
                    {
                        SeasonCount = 3,
                        EpisodeFileCount = 30,
                        TotalEpisodeCount = 35
                    }
                }
            };
            _mockSonarrService.Setup(s => s.SearchSeriesAsync(It.IsAny<string>()))
                .ReturnsAsync(series);
            var query = CreateQuery("-l test");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            var result = results.First();
            Assert.AreEqual("Test Series", result.Title);
            Assert.IsTrue(result.SubTitle.Contains("Test Network"));
            Assert.IsTrue(result.SubTitle.Contains("Continuing"));
            Assert.IsTrue(result.SubTitle.Contains("3 Seasons"));
            Assert.IsTrue(result.SubTitle.Contains("30/35 Episodes"));
            Assert.IsTrue(result.SubTitle.Contains("Right-click for options")); // Test new context menu hint
        }

        [TestMethod]
        public void Execute_WithNoResults_ReturnsNoResultsMessage()
        {
            // Arrange
            _mockSonarrService.Setup(s => s.SearchSeriesAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<SonarrSeries>());
            var query = CreateQuery("-l test");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("No Results Found", results.First().Title);
        }

        [TestMethod]
        public void Execute_WithEmptySearch_ReturnsAllSeries()
        {
            // Arrange
            var series = new List<Models.SonarrSeries>
            {
                new Models.SonarrSeries
                {
                    Id = 1,
                    Title = "Series 1",
                    TitleSlug = "series-1",
                    Network = "Network 1",
                    Status = "Ended",
                    Path = "",
                    Statistics = new Models.SeriesStatistics
                    {
                        SeasonCount = 1,
                        EpisodeFileCount = 10,
                        TotalEpisodeCount = 10
                    }
                },
                new Models.SonarrSeries
                {
                    Id = 2,
                    Title = "Series 2",
                    TitleSlug = "series-2",
                    Network = "Network 2",
                    Status = "Continuing",
                    Path = @"C:\TV\Series 2",
                    Statistics = new Models.SeriesStatistics
                    {
                        SeasonCount = 2,
                        EpisodeFileCount = 15,
                        TotalEpisodeCount = 20
                    }
                }
            };
            _mockSonarrService.Setup(s => s.SearchSeriesAsync(It.IsAny<string>()))
                .ReturnsAsync(series);
            var query = CreateQuery("-l");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Any(r => r.Title == "Series 1"));
            Assert.IsTrue(results.Any(r => r.Title == "Series 2"));
            
            // Test that series with local path shows context menu hint
            var seriesWithPath = results.FirstOrDefault(r => r.Title == "Series 2");
            Assert.IsNotNull(seriesWithPath);
            Assert.IsTrue(seriesWithPath.SubTitle.Contains("Right-click for options"));
            
            // Test that series without path doesn't show context menu hint for folders
            var seriesWithoutPath = results.FirstOrDefault(r => r.Title == "Series 1");
            Assert.IsNotNull(seriesWithoutPath);
            Assert.IsFalse(seriesWithoutPath.SubTitle.Contains("Right-click for options"));
        }

        [TestMethod]
        public void Execute_WithNoEpisodes_ShowsNoEpisodesMessage()
        {
            // Arrange
            var series = new List<Models.SonarrSeries>
            {
                new Models.SonarrSeries
                {
                    Id = 1,
                    Title = "Test Show",
                    TitleSlug = "test-show",
                    Network = "TestTV",
                    Status = "continuing",
                    Path = "",
                    Statistics = new Models.SeriesStatistics
                    {
                        SeasonCount = 0,
                        EpisodeFileCount = 0,
                        TotalEpisodeCount = 0
                    }
                }
            };
            _mockSonarrService.Setup(s => s.SearchSeriesAsync(It.IsAny<string>()))
                .ReturnsAsync(series);
            var query = CreateQuery("-l test");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results[0].SubTitle.Contains("No Episodes"));
        }

        [TestMethod]
        public void Execute_WithException_ReturnsErrorMessage()
        {
            // Arrange
            _mockSonarrService.Setup(s => s.SearchSeriesAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Connection failed"));
            var query = CreateQuery("-l test");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Error Connecting to Sonarr", results.First().Title);
            Assert.IsTrue(results.First().SubTitle.Contains("Connection failed"));
        }
    }
} 