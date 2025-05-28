using Microsoft.VisualStudio.TestTools.UnitTesting;
using Flow.Launcher.Plugin;
using System.Windows.Controls;

namespace SonarrFlowLauncherPlugin.Tests
{
    [TestClass]
    public class MainTests
    {
        private Main _plugin;

        [TestInitialize]
        public void Setup()
        {
            _plugin = new Main();
            _plugin.Init(new PluginInitContext());
        }

        [TestMethod]
        public void Query_NoApiKey_ReturnsSettingsError()
        {
            // Arrange
            var query = new Query("test");

            // Act
            var results = _plugin.Query(query);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Sonarr API Key Not Set", results[0].Title);
        }

        [TestMethod]
        public void CreateSettingPanel_ReturnsSettingsControl()
        {
            // Act
            var control = _plugin.CreateSettingPanel();

            // Assert
            Assert.IsInstanceOfType(control, typeof(Control));
            Assert.IsInstanceOfType(control, typeof(SettingsControl));
        }
    }
} 