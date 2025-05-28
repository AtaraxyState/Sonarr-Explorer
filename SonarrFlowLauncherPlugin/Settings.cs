using System.Windows.Controls;
using Flow.Launcher.Plugin;
using YamlDotNet.Serialization;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace SonarrFlowLauncherPlugin
{
    public class Settings
    {
        private static readonly string SettingsPath = Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location), "plugin.yaml");

        public string ApiKey { get; set; } = string.Empty;
        public string ServerUrl { get; set; } = "localhost:8989";
        public bool UseHttps { get; set; } = false;

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

        public void Save()
        {
            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(this);
            File.WriteAllText(SettingsPath, yaml);
        }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                return false;

            if (string.IsNullOrWhiteSpace(ServerUrl))
                return false;

            return true;
        }
    }

    public class SettingsControl : UserControl
    {
        private readonly Settings _settings;
        private readonly TextBox _apiKeyBox;
        private readonly TextBox _serverUrlBox;
        private readonly CheckBox _useHttpsBox;
        private readonly Label _statusLabel;

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

            Content = panel;

            // Initial validation
            ValidateAndUpdateStatus();
        }

        private void ValidateAndUpdateStatus()
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _statusLabel.Content = "Please enter your Sonarr API key";
                _statusLabel.Foreground = Brushes.Red;
                return;
            }

            if (string.IsNullOrWhiteSpace(_settings.ServerUrl))
            {
                _statusLabel.Content = "Please enter your Sonarr server URL";
                _statusLabel.Foreground = Brushes.Red;
                return;
            }

            _statusLabel.Content = "Settings saved and validated";
            _statusLabel.Foreground = Brushes.Green;
        }
    }
} 