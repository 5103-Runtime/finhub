namespace FinHub.Automation.Core.Config;

/// <summary>
/// An interface for adapting configuration settings for the
/// Reqnroll automation framework from external sources.
/// </summary>
public interface IConfigAdapter
{
    /// <summary>
    /// Gets the configured test environment.
    /// </summary>
    string TestEnvironment { get; }

    /// <summary>
    /// Gets the configured browser type.
    /// </summary>
    BrowserType Browser { get; }

    /// <summary>
    /// Gets the configured default timeout in seconds for waiting operations.
    /// </summary>
    int DefaultTimeout { get; }

    /// <summary>
    /// Gets the configured target project for the automation tests.
    /// </summary>
    string TargetProject { get; }
}