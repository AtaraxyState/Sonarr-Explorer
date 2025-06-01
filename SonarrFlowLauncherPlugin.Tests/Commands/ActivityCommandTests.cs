using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Commands;
using SonarrFlowLauncherPlugin.Models;
using SonarrFlowLauncherPlugin.Services;

namespace SonarrFlowLauncherPlugin.Tests.Commands
{
    [TestClass]
    public class ActivityCommandTests
    {
        private Mock<SonarrService> _mockSonarrService;
        private Settings _settings;
        private ActivityCommand _command;

        [TestInitialize]
        public void Setup()
        {
            _mockSonarrService = new Mock<SonarrService>(null);
            _settings = new Settings { ApiKey = "test-api-key" };
            _command = new ActivityCommand(_mockSonarrService.Object, _settings);
        }

        [TestMethod]
        public void Execute_NoApiKey_ReturnsSettingsError()
        {
            // Arrange
            _settings.ApiKey = "";
            var query = new Query("-a");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Sonarr API Key Not Set", results[0].Title);
        }

        [TestMethod]
        public void Execute_NoActivity_ReturnsNoActivityMessage()
        {
            // Arrange
            var activity = new SonarrActivity();
            _mockSonarrService.Setup(s => s.GetActivityAsync())
                .ReturnsAsync(activity);
            var query = new Query("-a");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(2, results.Count); // No activity message + Open in browser
            Assert.AreEqual("No Recent Activity", results[0].Title);
        }

        [TestMethod]
        public void Execute_WithQueueItems_ShowsQueueItems()
        {
            // Arrange
            var activity = new SonarrActivity
            {
                Queue = new List<SonarrQueueItem>
                {
                    new SonarrQueueItem
                    {
                        Title = "Test Show",
                        SeasonNumber = 1,
                        EpisodeNumber = 1,
                        Status = "downloading",
                        Progress = 50.5,
                        Quality = "WEBDL-1080p"
                    }
                }
            };
            _mockSonarrService.Setup(s => s.GetActivityAsync())
                .ReturnsAsync(activity);
            var query = new Query("-a");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(2, results.Count); // 1 queue item + Open in browser
            Assert.IsTrue(results[0].Title.Contains("Test Show"));
            Assert.IsTrue(results[0].SubTitle.Contains("S01E01"));
            Assert.IsTrue(results[0].SubTitle.Contains("50.5%"));
        }

        [TestMethod]
        public void Execute_WithHistoryItems_ShowsHistoryItems()
        {
            // Arrange
            var activity = new SonarrActivity
            {
                History = new List<SonarrHistoryItem>
                {
                    new SonarrHistoryItem
                    {
                        Title = "Test Show",
                        SeasonNumber = 1,
                        EpisodeNumber = 1,
                        EventType = "downloadfolderimported",
                        Date = DateTime.Now
                    }
                }
            };
            _mockSonarrService.Setup(s => s.GetActivityAsync())
                .ReturnsAsync(activity);
            var query = new Query("-a");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(2, results.Count); // 1 history item + Open in browser
            Assert.IsTrue(results[0].Title.Contains("✅"));
            Assert.IsTrue(results[0].Title.Contains("Test Show"));
        }

        [TestMethod]
        public void Execute_MaxItems_ShowsAllItems()
        {
            // Arrange
            var activity = new SonarrActivity
            {
                Queue = Enumerable.Range(0, 15).Select(i => new SonarrQueueItem
                {
                    Title = $"Queue Show {i}",
                    SeasonNumber = 1,
                    EpisodeNumber = i,
                }).ToList(),
                History = Enumerable.Range(0, 15).Select(i => new SonarrHistoryItem
                {
                    Title = $"History Show {i}",
                    SeasonNumber = 1,
                    EpisodeNumber = i,
                    EventType = "downloadfolderimported"
                }).ToList()
            };
            _mockSonarrService.Setup(s => s.GetActivityAsync())
                .ReturnsAsync(activity);
            var query = new Query("-a");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(31, results.Count); // 30 items (15 queue + 15 history) + Open in browser
        }

        [TestMethod]
        public void Execute_ServiceError_ReturnsErrorMessage()
        {
            // Arrange
            _mockSonarrService.Setup(s => s.GetActivityAsync())
                .ThrowsAsync(new Exception("Test error"));
            var query = new Query("-a");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Error Getting Activity", results[0].Title);
            Assert.IsTrue(results[0].SubTitle.Contains("Test error"));
        }
    }
} 