namespace FinHub.Automation.Core.Helpers;

/// <summary>
/// A class that includes helper methods for handling file paths and directories.
/// </summary>
public static class PathHelper
{
    #region Private Attributes
    private static readonly string _timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
    private static string? _productName;
    private static string? _baseDirectory;
    #endregion

    #region Public Properties
    public static string? BaseDirectory => _baseDirectory;
    #endregion

    #region Public Methods
    /// <summary>
    /// Configures the base directory for storing test results based on the provided product name.
    /// </summary>
    /// <remarks>Should be called once at the beginning of the test run.</remarks>
    /// <param name="productName">The product name.</param>
    /// <exception cref="ArgumentNullException">Throws if the product name is null or empty.</exception>
    public static void Configure(string productName)
    {
        if (_productName != null)
        {
            return; // Already configured
        }

        if (string.IsNullOrEmpty(productName))
        {
            throw new ArgumentNullException(nameof(productName), "Product name must be provided.");
        }

        _productName = productName.Replace(" ", "");
        _baseDirectory = Path.Combine(
            Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName ?? Path.GetTempPath(),
            "TestResults",
            _productName);
    }

    /// <summary>
    /// Gets the full path to the directory used for storing reports, creating the directory if it does not already
    /// exist.
    /// </summary>
    /// <returns>The full path to the reports directory.</returns>
    public static string GetReportDirectoryPath()
    {
        string reportDir = Path.Combine(_baseDirectory!, _timestamp, "Reports");
        Directory.CreateDirectory(reportDir);
        return reportDir;
    }

    /// <summary>
    /// Gets the full path to the directory used for storing screenshots, creating the directory if it does not already
    /// exist.
    /// </summary>
    /// <returns>The full path to the screenshots directory.</returns>
    public static string GetScreenshotsDirectoryPath()
    {
        string screenshotsDir = Path.Combine(_baseDirectory!, _timestamp, "Screenshots");
        Directory.CreateDirectory(screenshotsDir);
        return screenshotsDir;
    }
    #endregion
}