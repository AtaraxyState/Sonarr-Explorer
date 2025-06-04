using Flow.Launcher.Plugin;
using SonarrFlowLauncherPlugin.Services;

namespace SonarrFlowLauncherPlugin.Commands
{
    /// <summary>
    /// Abstract base class for all plugin commands providing common functionality and structure.
    /// Implements the command pattern with shared settings validation, error handling, and service access.
    /// </summary>
    /// <remarks>
    /// All command implementations must inherit from this class and implement:
    /// - CommandFlag: The trigger string (e.g., "-r" for refresh)
    /// - CommandName: Human-readable command name for help displays
    /// - CommandDescription: Detailed description of command functionality
    /// - Execute: Main command logic implementation
    /// </remarks>
    public abstract class BaseCommand
    {
        /// <summary>
        /// Service for communicating with Sonarr API endpoints
        /// </summary>
        protected readonly SonarrService SonarrService;
        
        /// <summary>
        /// Plugin settings containing API configuration and user preferences
        /// </summary>
        protected readonly Settings Settings;

        /// <summary>
        /// Initializes a new instance of the BaseCommand with required dependencies.
        /// </summary>
        /// <param name="sonarrService">Service for Sonarr API communication</param>
        /// <param name="settings">Plugin settings instance</param>
        protected BaseCommand(SonarrService sonarrService, Settings settings)
        {
            SonarrService = sonarrService;
            Settings = settings;
        }

        /// <summary>
        /// Gets the command flag used to trigger this command (e.g., "-r", "-c", "-help").
        /// This is the prefix users type to invoke the command.
        /// </summary>
        public abstract string CommandFlag { get; }
        
        /// <summary>
        /// Gets the human-readable name of this command for display in help and UI.
        /// </summary>
        public abstract string CommandName { get; }
        
        /// <summary>
        /// Gets a detailed description of what this command does and how to use it.
        /// </summary>
        public abstract string CommandDescription { get; }
        
        /// <summary>
        /// Executes the command logic with the provided user query.
        /// </summary>
        /// <param name="query">User input query containing command parameters and search terms</param>
        /// <returns>List of results to display in Flow Launcher</returns>
        public abstract List<Result> Execute(Query query);

        /// <summary>
        /// Provides public access to the SonarrService for the CommandManager.
        /// Used for special command shortcuts that need direct service access.
        /// </summary>
        /// <returns>The SonarrService instance used by this command</returns>
        public SonarrService GetSonarrService() => SonarrService;

        /// <summary>
        /// Validates that required settings are configured for API-dependent commands.
        /// Checks for presence of API key which is required for most Sonarr operations.
        /// </summary>
        /// <returns>True if settings are valid for API calls, false if setup is required</returns>
        protected bool ValidateSettings()
        {
            if (string.IsNullOrEmpty(Settings.ApiKey))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Generates helpful error results when API settings are not configured.
        /// Provides multiple pathways for users to complete setup and configuration.
        /// </summary>
        /// <returns>List of results guiding user through setup process</returns>
        /// <remarks>
        /// Returns results for:
        /// - Quick setup wizard command
        /// - Manual settings panel access
        /// - Instructions for finding API key in Sonarr
        /// - Step-by-step quick start guide
        /// </remarks>
        protected List<Result> GetSettingsError()
        {
            return new List<Result>
            {
                new Result
                {
                    Title = "🔧 Setup Required: Sonarr API Key Not Set",
                    SubTitle = "Type 'snr -setup' to start guided setup wizard",
                    IcoPath = "Images\\icon.png",
                    Score = 100,
                    Action = _ => false
                },
                new Result
                {
                    Title = "⚙️ Alternative: Plugin Settings Panel",
                    SubTitle = "Open Flow Launcher Settings → Plugins → Sonarr Explorer to configure manually",
                    IcoPath = "Images\\icon.png",
                    Score = 95,
                    Action = _ => false
                },
                new Result
                {
                    Title = "❓ How to Find Your API Key",
                    SubTitle = "In Sonarr: Settings → General → API Key (copy the long string)",
                    IcoPath = "Images\\icon.png",
                    Score = 90,
                    Action = _ => false
                },
                new Result
                {
                    Title = "📖 Quick Start Guide",
                    SubTitle = "1. Get API key from Sonarr → 2. Type 'snr -setup' → 3. Follow wizard",
                    IcoPath = "Images\\icon.png",
                    Score = 85,
                    Action = _ => false
                }
            };
        }

        /// <summary>
        /// Attempts to open the plugin settings panel (placeholder for future implementation).
        /// Currently returns false as Flow Launcher settings cannot be reliably opened programmatically.
        /// </summary>
        /// <returns>Always false - user must manually access settings</returns>
        private bool OpenPluginSettings()
        {
            // Just return false since we can't reliably open Flow Launcher settings
            // The user will need to open it manually
            return false;
        }
    }
} 