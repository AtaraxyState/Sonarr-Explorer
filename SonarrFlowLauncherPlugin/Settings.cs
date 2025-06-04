using System.Windows.Controls;
using Flow.Launcher.Plugin;
using YamlDotNet.Serialization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Threading.Tasks;
using SonarrFlowLauncherPlugin.Services;
using System;

namespace SonarrFlowLauncherPlugin
{
    /// <summary>
    /// Configuration settings for the Sonarr Flow Launcher plugin.
    /// Handles serialization, validation, and persistence of user preferences and API connection details.
    /// </summary>
    /// <remarks>
    /// Settings are automatically saved to plugin.yaml when modified and loaded on plugin initialization.
    /// Supports hot-reloading when the settings file is externally modified.
    /// </remarks>
    public class Settings
    {
        /// <summary>
        /// File path for the plugin settings YAML file
        /// </summary>
        private static readonly string SettingsPath = Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location) ?? Environment.CurrentDirectory, "plugin.yaml");

        /// <summary>
        /// Sonarr API key for authentication with the Sonarr server.
        /// Required for all API operations. Can be found in Sonarr → Settings → General → API Key.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
        
        /// <summary>
        /// Sonarr server URL without protocol prefix (e.g., "localhost:8989" or "192.168.1.100:8989").
        /// Protocol is determined by the UseHttps setting.
        /// </summary>
        public string ServerUrl { get; set; } = "localhost:8989";
        
        /// <summary>
        /// Whether to use HTTPS for API connections. When false, HTTP is used.
        /// Default is false for typical local Sonarr installations.
        /// </summary>
        public bool UseHttps { get; set; } = false;

        /// <summary>
        /// Loads settings from the plugin.yaml file or creates default settings if file doesn't exist.
        /// </summary>
        /// <returns>Settings instance populated with saved values or defaults</returns>
        /// <remarks>
        /// If the settings file doesn't exist, a new one is created with default values.
        /// Uses YAML deserialization for human-readable configuration files.
        /// </remarks>
        public static Settings Load()
        {
            if (!File.Exists(SettingsPath))
            {
                var settings = new Settings();
                settings.Save();
                return settings;
            }

            var deserializer = new DeserializerBuilder().Build();
            var yaml = File.ReadAllText(SettingsPath);
            return deserializer.Deserialize<Settings>(yaml);
        }

        /// <summary>
        /// Saves current settings to the plugin.yaml file using YAML serialization.
        /// Called automatically when settings are modified through the UI.
        /// </summary>
        public void Save()
        {
            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(this);
            File.WriteAllText(SettingsPath, yaml);
        }

        /// <summary>
        /// Validates that required settings are present and properly configured.
        /// </summary>
        /// <returns>True if ApiKey and ServerUrl are both non-empty, false otherwise</returns>
        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                return false;

            if (string.IsNullOrWhiteSpace(ServerUrl))
                return false;

            return true;
        }
    }

    /// <summary>
    /// WPF UserControl for configuring plugin settings within Flow Launcher's settings interface.
    /// Provides real-time validation, automatic saving, and connection testing functionality.
    /// </summary>
    /// <remarks>
    /// Features include:
    /// - Real-time input validation with visual feedback
    /// - Automatic settings persistence on change
    /// - Connection testing with detailed error reporting
    /// - User-friendly input formatting and validation
    /// - Clear instructions for finding API key in Sonarr
    /// </remarks>
    public class SettingsControl : UserControl
    {
        /// <summary>
        /// Reference to the settings instance being configured
        /// </summary>
        private readonly Settings _settings;
        
        /// <summary>
        /// TextBox for entering the Sonarr API key
        /// </summary>
        private readonly TextBox _apiKeyBox;
        
        /// <summary>
        /// TextBox for entering the Sonarr server URL
        /// </summary>
        private readonly TextBox _serverUrlBox;
        
        /// <summary>
        /// CheckBox for enabling/disabling HTTPS connections
        /// </summary>
        private readonly CheckBox _useHttpsBox;
        
        /// <summary>
        /// Label for displaying validation status and connection test results
        /// </summary>
        private readonly Label _statusLabel;
        
        /// <summary>
        /// Button for manually testing the connection to Sonarr
        /// </summary>
        private readonly Button _testConnectionButton;
        
        /// <summary>
        /// Temporary service instance used for connection testing
        /// </summary>
        private SonarrService? _testService;

        /// <summary>
        /// Initializes a new instance of the SettingsControl with the specified settings.
        /// Creates the complete UI layout with input controls, validation, and help text.
        /// </summary>
        /// <param name="settings">Settings instance to bind to the UI controls</param>
        public SettingsControl(Settings settings)
        {
            _settings = settings;

            var panel = new StackPanel { Margin = new System.Windows.Thickness(10) };
            
            // API Key
            panel.Children.Add(new Label { Content = "API Key:" });
            _apiKeyBox = new TextBox
            {
                Text = settings.ApiKey,
                Margin = new System.Windows.Thickness(0, 0, 0, 10)
            };
            _apiKeyBox.TextChanged += (s, e) => 
            {
                settings.ApiKey = _apiKeyBox.Text;
                settings.Save();
                ValidateAndUpdateStatus();
            };
            panel.Children.Add(_apiKeyBox);

            // Server URL
            panel.Children.Add(new Label { Content = "Server URL:" });
            _serverUrlBox = new TextBox
            {
                Text = settings.ServerUrl,
                Margin = new System.Windows.Thickness(0, 0, 0, 10)
            };
            _serverUrlBox.TextChanged += (s, e) => 
            {
                // Remove any protocol prefix if user enters it
                var url = _serverUrlBox.Text.Replace("http://", "").Replace("https://", "");
                settings.ServerUrl = url;
                _serverUrlBox.Text = url; // Update textbox if we modified the URL
                settings.Save();
                ValidateAndUpdateStatus();
            };
            panel.Children.Add(_serverUrlBox);

            // Use HTTPS
            _useHttpsBox = new CheckBox
            {
                Content = "Use HTTPS",
                IsChecked = settings.UseHttps,
                Margin = new System.Windows.Thickness(0, 0, 0, 10)
            };
            _useHttpsBox.Checked += (s, e) => 
            {
                settings.UseHttps = true;
                settings.Save();
                ValidateAndUpdateStatus();
            };
            _useHttpsBox.Unchecked += (s, e) => 
            {
                settings.UseHttps = false;
                settings.Save();
                ValidateAndUpdateStatus();
            };
            panel.Children.Add(_useHttpsBox);

            // Test Connection Button
            _testConnectionButton = new Button
            {
                Content = "Test Connection",
                Margin = new System.Windows.Thickness(0, 0, 0, 10),
                Padding = new System.Windows.Thickness(10, 5, 10, 5),
                IsEnabled = false
            };
            _testConnectionButton.Click += async (s, e) => await TestConnectionAsync();
            panel.Children.Add(_testConnectionButton);

            // Status Label
            _statusLabel = new Label
            {
                Content = "Please enter your Sonarr API key and server URL",
                Foreground = Brushes.Gray,
                Margin = new System.Windows.Thickness(0, 10, 0, 0)
            };
            panel.Children.Add(_statusLabel);

            // Example Label
            var exampleLabel = new Label
            {
                Content = "Example URL: localhost:8989",
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray,
                Margin = new System.Windows.Thickness(0, 5, 0, 0)
            };
            panel.Children.Add(exampleLabel);

            // Instructions Label
            var instructionsLabel = new Label
            {
                Content = "Find your API key in Sonarr → Settings → General → API Key",
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray,
                Margin = new System.Windows.Thickness(0, 5, 0, 0)
            };
            panel.Children.Add(instructionsLabel);

            Content = panel;

            // Initial validation
            ValidateAndUpdateStatus();
        }

        /// <summary>
        /// Validates current input values and updates the status label with appropriate feedback.
        /// Controls the enabled state of the test connection button based on validation results.
        /// </summary>
        /// <remarks>
        /// Provides immediate visual feedback for:
        /// - Missing API key (red text, disabled test button)
        /// - Missing server URL (red text, disabled test button)
        /// - Valid configuration (orange text, enabled test button)
        /// </remarks>
        private void ValidateAndUpdateStatus()
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _statusLabel.Content = "Please enter your Sonarr API key";
                _statusLabel.Foreground = Brushes.Red;
                _testConnectionButton.IsEnabled = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(_settings.ServerUrl))
            {
                _statusLabel.Content = "Please enter your Sonarr server URL";
                _statusLabel.Foreground = Brushes.Red;
                _testConnectionButton.IsEnabled = false;
                return;
            }

            _statusLabel.Content = "Settings saved - Click 'Test Connection' to verify";
            _statusLabel.Foreground = Brushes.Orange;
            _testConnectionButton.IsEnabled = true;
        }

        /// <summary>
        /// Performs an asynchronous connection test to the configured Sonarr server.
        /// Updates UI with detailed success/failure feedback and manages button state during testing.
        /// </summary>
        /// <returns>Task representing the asynchronous connection test operation</returns>
        /// <remarks>
        /// The test procedure:
        /// 1. Disables test button and shows "Testing..." status
        /// 2. Creates temporary SonarrService with current settings
        /// 3. Attempts to retrieve series list from Sonarr API
        /// 4. Displays success message with series count or detailed error
        /// 5. Cleans up temporary service and re-enables button
        /// </remarks>
        private async Task TestConnectionAsync()
        {
            try
            {
                _testConnectionButton.IsEnabled = false;
                _testConnectionButton.Content = "Testing...";
                _statusLabel.Content = "Testing connection to Sonarr...";
                _statusLabel.Foreground = Brushes.Blue;

                // Dispose previous test service if it exists
                _testService?.Dispose();

                // Create a test service with current settings
                _testService = new SonarrService(_settings);

                // Test by trying to get a small amount of data from Sonarr
                var series = await _testService.SearchSeriesAsync("");
                
                // If we get here, the connection worked
                _statusLabel.Content = $"✅ Connection successful! Found {series.Count} series in library";
                _statusLabel.Foreground = Brushes.Green;
            }
            catch (Exception ex)
            {
                _statusLabel.Content = $"❌ Connection failed: {ex.Message}";
                _statusLabel.Foreground = Brushes.Red;
            }
            finally
            {
                _testConnectionButton.Content = "Test Connection";
                _testConnectionButton.IsEnabled = true;
                
                // Clean up test service
                _testService?.Dispose();
                _testService = null;
            }
        }
    }
} 