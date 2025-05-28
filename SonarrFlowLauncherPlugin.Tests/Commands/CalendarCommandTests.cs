using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Commands;
using SonarrFlowLauncherPlugin.Models;
using SonarrFlowLauncherPlugin.Services;
using System;
using System.Collections.Generic;

namespace SonarrFlowLauncherPlugin.Tests.Commands
{
    [TestClass]
    public class CalendarCommandTests
    {
        private Mock<SonarrService> _mockSonarrService;
        private Settings _settings;
        private CalendarCommand _command;

        [TestInitialize]
        public void Setup()
        {
            _mockSonarrService = new Mock<SonarrService>(null);
            _settings = new Settings { ApiKey = "test-api-key" };
            _command = new CalendarCommand(_mockSonarrService.Object, _settings);
        }

        [TestMethod]
        public void Execute_NoApiKey_ReturnsSettingsError()
        {
            // Arrange
            _settings.ApiKey = "";
            var query = new Query("-c");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Sonarr API Key Not Set", results[0].Title);
        }

        [TestMethod]
        public void Execute_NoQuery_ShowsOptions()
        {
            // Arrange
            _mockSonarrService.Setup(s => s.GetCalendarAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<SonarrCalendarItem>());
            var query = new Query("-c");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.IsTrue(results.Count >= 2); // Options + No Episodes + Open in Browser
            Assert.IsTrue(results.Exists(r => r.Title == "Calendar Options"));
        }

        [TestMethod]
        public void Execute_Today_ShowsTodayEpisodes()
        {
            // Arrange
            var episodes = new List<SonarrCalendarItem>
            {
                new SonarrCalendarItem
                {
                    Title = "Test Show",
                    EpisodeTitle = "Test Episode",
                    SeasonNumber = 1,
                    EpisodeNumber = 1,
                    AirDate = DateTime.Today,
                    HasFile = false,
                    Monitored = true
                }
            };
            _mockSonarrService.Setup(s => s.GetCalendarAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(episodes);
            var query = new Query("-c today");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.IsTrue(results.Count >= 3); // Date header + Episode + Open in Browser
            Assert.IsTrue(results.Exists(r => r.Title.Contains("Today")));
            Assert.IsTrue(results.Exists(r => r.Title.Contains("Test Show")));
        }

        [TestMethod]
        public void Execute_NoEpisodes_ShowsNoEpisodesMessage()
        {
            // Arrange
            _mockSonarrService.Setup(s => s.GetCalendarAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<SonarrCalendarItem>());
            var query = new Query("-c today");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.IsTrue(results.Count >= 2); // No Episodes + Open in Browser
            Assert.IsTrue(results.Exists(r => r.Title == "No Episodes Found"));
        }

        [TestMethod]
        public void Execute_ServiceError_ReturnsErrorMessage()
        {
            // Arrange
            _mockSonarrService.Setup(s => s.GetCalendarAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ThrowsAsync(new Exception("Test error"));
            var query = new Query("-c");

            // Act
            var results = _command.Execute(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Error Getting Calendar", results[0].Title);
            Assert.IsTrue(results[0].SubTitle.Contains("Test error"));
        }
    }
} 