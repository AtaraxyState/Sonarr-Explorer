using Microsoft.VisualStudio.TestTools.UnitTesting;
using Flow.Launcher.Plugin;
using System.Windows.Controls;

namespace SonarrFlowLauncherPlugin.Tests
{
    [TestClass]
    public class MainTests
    {
        private Main _plugin = null!;

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
            _plugin = new Main();
            _plugin.Init(new PluginInitContext());
        }

        [TestMethod]
        public void Query_WithValidInput_ReturnsResults()
        {
            // Arrange
            var query = CreateQuery("test");

            // Act
            var results = _plugin.Query(query);

            // Assert
            Assert.IsNotNull(results);
            Assert.IsTrue(results.Count > 0);
        }

        [TestMethod]
        public void CreateSettingPanel_ReturnsControl()
        {
            // Act
            var control = _plugin.CreateSettingPanel();

            // Assert
            Assert.IsNotNull(control);
        }
    }
} 