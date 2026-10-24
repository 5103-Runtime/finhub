using Microsoft.Extensions.Configuration;
using FinHub.Automation.Core.Config;

namespace FinHub.Automation.Config;

/// <summary>
/// A class that manages the configuration settings for the Reqnroll automation framework.
/// </summary>
internal static class ConfigManager
{
    // Private attributes
    private static readonly IConfigurationRoot _configEngine;
    private const string ConfigFileName = "config.json";

    #region Public Properties
    /// <summary>
    /// Gets the base URL of the application under test.
    /// </summary>
    public static string BaseUrl => GetValue(nameof(BaseUrl), "http://localhost:5173").TrimEnd('/');

    /// <summary>
    /// Gets the configured environment, defaulting to "QA" if not specified.
    /// </summary>
    public static string TestEnvironment => GetValue(nameof(TestEnvironment), "QA");

    /// <summary>
    /// Gets the default timeout value in seconds, defaulting to 30 if not specified.
    /// </summary>
    public static int DefaultTimeout => GetValue(nameof(DefaultTimeout), 30);

    /// <summary>
    /// Gets the configured browser type, defaulting to Chrome if not specified.
    /// </summary>
    public static BrowserType Browser
    {
        get
        {
            string browserString = GetValue("Browser", "Chrome");

            return Enum.TryParse(browserString, true, out BrowserType browserType)
                ? browserType
                : BrowserType.Chrome;
        }
    }

    /// <summary>
    /// Gets the configured headless mode setting, defaulting to true if not specified or if running in a CI environment.
    /// </summary>
    public static bool Headless
    {
        get
        {
            return GetValue(nameof(Headless), true) ||
                   Environment.GetEnvironmentVariable("HEADLESS") == "true" ||
                   Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" ||
                   Environment.GetEnvironmentVariable("CI") == "true";
        }
    }

    /// <summary>
    /// Gets the configured target project name, defaulting to "Reqnroll" if not specified.
    /// </summary>
    public static string TargetProject => GetValue(nameof(TargetProject), "Reqnroll");
    #endregion

    // Constructor
    static ConfigManager()
    {
        // Determine the path to the Config\config.json file on the project root directory
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        string configDirectory = Path.Combine(projectRoot, "Config");
        string jsonPath = Path.Combine(configDirectory, ConfigFileName);

        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"[ERROR] Could not find {ConfigFileName} at: {jsonPath}");
        }

        // Build the configuration from the config.json file
        _configEngine = new ConfigurationBuilder()
            .SetBasePath(configDirectory)
            .AddJsonFile(ConfigFileName, optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    // Public methods
    /// <summary>
    /// Retrieves a configuration value of type T from the configuration settings using the specified key or path.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="keyOrPath">The key or path of the configuration value.</param>
    /// <param name="defaultValue">The default value to return if the configuration value is not found.</param>
    /// <returns>The configuration value or the default value if not found.</returns>
    public static T GetValue<T>(string keyOrPath, T defaultValue = default!)
    {
        try
        {
            IConfigurationSection section = _configEngine.GetSection(keyOrPath);

            if (!section.Exists())
            {
                return defaultValue;
            }

            return section.Get<T>() ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }
}
