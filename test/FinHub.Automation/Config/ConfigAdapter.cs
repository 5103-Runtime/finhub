using FinHub.Automation.Core.Config;

namespace FinHub.Automation.Config;

/// <summary>
/// An interface for adapting configuration settings for the 
/// Reqnroll automation framework from external sources.
/// </summary>
internal class ConfigAdapter : IConfigAdapter
{
    /// <inheritdoc/>
    public string TestEnvironment => ConfigManager.TestEnvironment;

    /// <inheritdoc/>
    public BrowserType Browser => ConfigManager.Browser;

    /// <inheritdoc/>
    public int DefaultTimeout => ConfigManager.DefaultTimeout;

    /// <inheritdoc/>
    public string TargetProject => ConfigManager.TargetProject;
}