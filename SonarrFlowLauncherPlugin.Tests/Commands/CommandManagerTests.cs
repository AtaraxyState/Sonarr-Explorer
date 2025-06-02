using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Commands;
using SonarrFlowLauncherPlugin.Services;

namespace SonarrFlowLauncherPlugin.Tests.Commands
{
    [TestClass]
    public class CommandManagerTests
    {
        private Mock<SonarrService> _mockSonarrService;
        private Settings _settings;
        private CommandManager _commandManager;

        [TestInitialize]
        public void Setup()
        {
            _mockSonarrService = new Mock<SonarrService>(null);
            _settings = new Settings { ApiKey = "test-api-key" };
            _commandManager = new CommandManager(_mockSonarrService.Object, _settings);
        }

        [TestMethod]
        public void HandleQuery_EmptyQuery_ReturnsAvailableCommands()
        {
            // Arrange
            var query = new Query("");

            // Act
            var results = _commandManager.HandleQuery(query);

            // Assert
            Assert.AreEqual(4, results.Count); // Activity, Library Search, Calendar, and Refresh commands
            Assert.IsTrue(results.Any(r => r.Title == "View Sonarr Activity"));
            Assert.IsTrue(results.Any(r => r.Title == "Search Sonarr Library"));
            Assert.IsTrue(results.Any(r => r.Title == "View Sonarr Calendar"));
            Assert.IsTrue(results.Any(r => r.Title == "Refresh Sonarr Series"));
        }

        [TestMethod]
        public void HandleQuery_ActivityFlag_ExecutesActivityCommand()
        {
            // Arrange
            _mockSonarrService.Setup(s => s.GetActivityAsync())
                .ReturnsAsync(new Models.SonarrActivity());
            var query = new Query("-a");

            // Act
            var results = _commandManager.HandleQuery(query);

            // Assert
            Assert.IsTrue(results.Any(r => r.Title == "No Recent Activity"));
            _mockSonarrService.Verify(s => s.GetActivityAsync(), Times.Once);
        }

        [TestMethod]
        public void HandleQuery_LibrarySearchFlag_ExecutesLibrarySearchCommand()
        {
            // Arrange
            _mockSonarrService.Setup(s => s.SearchSeriesAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<Models.SonarrSeries>());
            var query = new Query("-l test");

            // Act
            var results = _commandManager.HandleQuery(query);

            // Assert
            Assert.IsTrue(results.Any(r => r.Title == "No Results Found"));
            _mockSonarrService.Verify(s => s.SearchSeriesAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void HandleQuery_NoFlag_ExecutesDefaultLibrarySearch()
        {
            // Arrange
            _mockSonarrService.Setup(s => s.SearchSeriesAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<Models.SonarrSeries>());
            var query = new Query("test");

            // Act
            var results = _commandManager.HandleQuery(query);

            // Assert
            Assert.IsTrue(results.Any(r => r.Title == "No Results Found"));
            _mockSonarrService.Verify(s => s.SearchSeriesAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void HandleQuery_UnknownFlag_ExecutesDefaultLibrarySearch()
        {
            // Arrange
            _mockSonarrService.Setup(s => s.SearchSeriesAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<Models.SonarrSeries>());
            var query = new Query("-x test");

            // Act
            var results = _commandManager.HandleQuery(query);

            // Assert
            Assert.IsTrue(results.Any(r => r.Title == "No Results Found"));
            _mockSonarrService.Verify(s => s.SearchSeriesAsync(It.IsAny<string>()), Times.Once);
        }
    }
} 