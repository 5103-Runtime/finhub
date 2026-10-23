namespace FinHub.Automation.Core.Config;

/// <summary>
/// Specifies the type of browser to be used for testing.
/// </summary>
public enum BrowserType
{
    Chrome,
    Edge
}

/// <summary>
/// A class that provides configuration settings for the Reqnroll 
/// automation framework from the external IConfigAdapter.
/// </summary>
public static class ConfigProvider
{
    // Private Property
    /// <summary>
    /// The internal configuration adapter.
    /// </summary>
    private static IConfigAdapter ConfigAdapter
    {
        get => field ?? throw new InvalidOperationException("ConfigProvider is not initialized. Call Initialize() with a valid IConfigAdapter.");
        set => field ??= value ?? throw new ArgumentNullException(nameof(value), "Config adapter cannot be null.");
    }

    #region Public Properties
    /// <summary>
    /// Gets the configured test environment.
    /// </summary>
    public static string TestEnvironment => ConfigAdapter.TestEnvironment;

    /// <summary>
    /// Gets the configured browser type.
    /// </summary>
    public static BrowserType Browser => ConfigAdapter.Browser;
    /// <summary>
    /// Gets the configured default timeout in seconds for waiting operations.
    /// </summary>
    public static int DefaultTimeout => ConfigAdapter.DefaultTimeout;

    /// <summary>
    /// Gets the configured target project for the automation tests.
    /// </summary>
    public static string TargetProject => ConfigAdapter.TargetProject;
    #endregion

    // Public methods
    /// <summary>
    /// Initializes the ConfigProvider with the specified IConfigAdapter.
    /// </summary>
    /// <param name="adapter">The IConfigAdapter to use for configuration settings.</param>
    public static void Initialize(IConfigAdapter adapter) => ConfigAdapter = adapter;
}