using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Commands;
using SonarrFlowLauncherPlugin.Models;
using SonarrFlowLauncherPlugin.Services;

namespace SonarrFlowLauncherPlugin.Tests.Commands
{
    [TestClass]
    public class RefreshCommandTests
    {
        private Mock<SonarrService> _mockSonarrService;
        private Settings _settings;
        private RefreshCommand _command;

        [TestInitialize]
        public void Setup()
        {
            _mockSonarrService = new Mock<SonarrService>(null);
            _settings = new Settings { ApiKey = "test-api-key" };
            _command = new RefreshCommand(_mockSonarrService.Object, _settings);
        }

        [TestMethod]
        public void Execute_NoApiKey_ReturnsSettingsError()
        {
            // Arrange
            _settings.ApiKey = "";
            var query = new Query("-r");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Sonarr API Key Not Set", results[0].Title);
        }

        [TestMethod]
        public void Execute_NoQuery_ShowsRefreshAllAndOptions()
        {
            // Arrange
            _mockSonarrService.Setup(s => s.RefreshAllSeriesAsync())
                .ReturnsAsync(true);
            var query = new Query("-r");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("Refresh All Series", results[0].Title);
            Assert.AreEqual("Refresh Options", results[1].Title);
        }

        [TestMethod]
        public void Execute_AllQuery_ShowsRefreshAllOnly()
        {
            // Arrange
            _mockSonarrService.Setup(s => s.RefreshAllSeriesAsync())
                .ReturnsAsync(true);
            var query = new Query("-r all");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Refresh All Series", results[0].Title);
        }

        [TestMethod]
        public void Execute_SeriesSearch_ReturnsMatchingSeries()
        {
            // Arrange
            var testSeries = new List<SonarrSeries>
            {
                new SonarrSeries { Id = 1, Title = "Test Show", Status = "continuing" },
                new SonarrSeries { Id = 2, Title = "Another Test", Status = "ended" }
            };

            _mockSonarrService.Setup(s => s.SearchSeriesAsync("test"))
                .ReturnsAsync(testSeries);
            _mockSonarrService.Setup(s => s.RefreshSeriesAsync(It.IsAny<int>()))
                .ReturnsAsync(true);

            var query = new Query("-r test");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(3, results.Count); // 2 series + refresh all option
            Assert.AreEqual("Refresh: Test Show", results[0].Title);
            Assert.AreEqual("Refresh: Another Test", results[1].Title);
            Assert.AreEqual("Refresh All Series", results[2].Title);
        }

        [TestMethod]
        public void Execute_NoMatchingSeries_ShowsNoResultsMessage()
        {
            // Arrange
            _mockSonarrService.Setup(s => s.SearchSeriesAsync("nonexistent"))
                .ReturnsAsync(new List<SonarrSeries>());
            _mockSonarrService.Setup(s => s.RefreshAllSeriesAsync())
                .ReturnsAsync(true);

            var query = new Query("-r nonexistent");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("No Series Found", results[0].Title);
            Assert.AreEqual("Refresh All Series", results[1].Title);
        }

        [TestMethod]
        public void CommandProperties_AreCorrect()
        {
            // Assert
            Assert.AreEqual("-r", _command.CommandFlag);
            Assert.AreEqual("Refresh Sonarr Series", _command.CommandName);
            Assert.IsTrue(_command.CommandDescription.Contains("Refresh all series"));
        }
    }
} 