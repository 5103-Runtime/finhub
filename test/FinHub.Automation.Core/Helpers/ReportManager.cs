using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using AventStack.ExtentReports.Reporter.Config;
using FinHub.Automation.Core.Config;

namespace FinHub.Automation.Core.Helpers;

/// <summary>
/// A class that manages the creation and configuration of ExtentReports
/// for test reporting in the Reqnroll automation framework.
/// </summary>
public static class ReportManager
{
    #region Private Attributes
    private static ExtentReports? _extentReports;
    private static ExtentSparkReporter? _extentSparkReporter;
    private static string _reportPath = "";
    private static readonly Lock _flushLock = new();
    #endregion

    #region Public Properties
    public static string ReportPath => _reportPath;
    public static string? ProductName { get; private set; }
    #endregion

    #region Public Methods
    /// <summary>
    /// Initializes the ExtentReports and configures the report settings.
    /// </summary>
    public static void InitializeReport()
    {
        // If the report is already initialized, skip re-initialization
        if (ProductName != null)
        {
            return; // Report is already initialized
        }

        string productName = ConfigProvider.TargetProject;
        ProductName = productName;

        // Set up the report path
        PathHelper.Configure(productName);
        string reportDir = PathHelper.GetReportDirectoryPath();
        _reportPath = Path.Combine(reportDir, "ExtentReport.html");
        Console.WriteLine($"Extent Report Path: {_reportPath}");

        // Set up the ExtentSparkReporter
        _extentSparkReporter = new(_reportPath);
        _extentSparkReporter.Config.DocumentTitle = $"{ProductName} Automation Test Report";
        _extentSparkReporter.Config.ReportName = $"{ProductName} Automation Tests";
        _extentSparkReporter.Config.Theme = Theme.Dark;

        // Set up the ExtentReports
        _extentReports = new();
        _extentReports.AttachReporter(_extentSparkReporter);

        _extentReports.AddSystemInfo("Environment", ConfigProvider.TestEnvironment);
        _extentReports.AddSystemInfo("Project", ConfigProvider.TargetProject);
        _extentReports.AddSystemInfo("Browser", ConfigProvider.Browser.ToString());
        _extentReports.AddSystemInfo("Tester", Environment.UserName);
        _extentReports.AddSystemInfo("OS", Environment.OSVersion.ToString());

        Console.WriteLine("[LOG] Report initialized successfully.");
    }

    /// <summary>
    /// Gets the singleton instance of the ExtentReports object used for reporting.
    /// </summary>
    /// <returns>The singleton instance of the ExtentReports.</returns>
    public static ExtentReports GetExtentReports()
    {
        if (_extentReports == null)
        {
            InitializeReport();
        }
        return _extentReports!;
    }

    /// <summary>
    /// Flushes and saves the report.
    /// </summary>
    public static void FlushReport()
    {
        try
        {
            lock (_flushLock)
            {
                _extentReports?.Flush();
            }
            Console.WriteLine("[LOG] Report saved successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Report failed to save: {ex.Message}");
        }
    }
    #endregion
}