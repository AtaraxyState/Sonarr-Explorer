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
        private Mock<SonarrService> _mockSonarrService;
        private Settings _settings;
        private LibrarySearchCommand _command;

        [TestInitialize]
        public void Setup()
        {
            _mockSonarrService = new Mock<SonarrService>(null);
            _settings = new Settings { ApiKey = "test-api-key" };
            _command = new LibrarySearchCommand(_mockSonarrService.Object, _settings);
        }

        [TestMethod]
        public void Execute_NoApiKey_ReturnsSettingsError()
        {
            // Arrange
            _settings.ApiKey = "";
            var query = new Query("-l test");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Sonarr API Key Not Set", results[0].Title);
        }

        [TestMethod]
        public void Execute_EmptySearch_ReturnsEnterSearchMessage()
        {
            // Arrange
            var query = new Query("-l");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Enter Search Term", results[0].Title);
        }

        [TestMethod]
        public void Execute_NoResults_ReturnsNoResultsMessage()
        {
            // Arrange
            _mockSonarrService.Setup(s => s.SearchSeriesAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<SonarrSeries>());
            var query = new Query("-l test");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("No Results Found", results[0].Title);
            Assert.IsTrue(results[0].SubTitle.Contains("test"));
        }

        [TestMethod]
        public void Execute_WithResults_ShowsSeriesResults()
        {
            // Arrange
            var series = new List<SonarrSeries>
            {
                new SonarrSeries
                {
                    Id = 1,
                    Title = "Test Show",
                    Network = "TestTV",
                    Status = "continuing",
                    Statistics = new SeriesStatistics
                    {
                        SeasonCount = 2,
                        EpisodeFileCount = 10,
                        TotalEpisodeCount = 20
                    }
                }
            };
            _mockSonarrService.Setup(s => s.SearchSeriesAsync(It.IsAny<string>()))
                .ReturnsAsync(series);
            var query = new Query("-l test");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Test Show", results[0].Title);
            Assert.IsTrue(results[0].SubTitle.Contains("TestTV"));
            Assert.IsTrue(results[0].SubTitle.Contains("continuing"));
            Assert.IsTrue(results[0].SubTitle.Contains("2 Seasons"));
            Assert.IsTrue(results[0].SubTitle.Contains("10/20 Episodes"));
        }

        [TestMethod]
        public void Execute_WithNoEpisodes_ShowsNoEpisodesMessage()
        {
            // Arrange
            var series = new List<SonarrSeries>
            {
                new SonarrSeries
                {
                    Id = 1,
                    Title = "Test Show",
                    Network = "TestTV",
                    Status = "continuing",
                    Statistics = new SeriesStatistics
                    {
                        SeasonCount = 0,
                        EpisodeFileCount = 0,
                        TotalEpisodeCount = 0
                    }
                }
            };
            _mockSonarrService.Setup(s => s.SearchSeriesAsync(It.IsAny<string>()))
                .ReturnsAsync(series);
            var query = new Query("-l test");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results[0].SubTitle.Contains("No Episodes"));
        }

        [TestMethod]
        public void Execute_ServiceError_ReturnsErrorMessage()
        {
            // Arrange
            _mockSonarrService.Setup(s => s.SearchSeriesAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test error"));
            var query = new Query("-l test");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Error Connecting to Sonarr", results[0].Title);
            Assert.IsTrue(results[0].SubTitle.Contains("Test error"));
        }
    }
} 