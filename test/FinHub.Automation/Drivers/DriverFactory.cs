
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.Edge;
using FinHub.Automation.Config;
using FinHub.Automation.Core.Config;

namespace FinHub.Automation.Drivers;

/// <summary>
/// A class that provides a factory for managing persistent, thread-safe WebDriver instances 
/// for parallel test execution using thread-static driver pooling.
/// </summary>
internal class DriverFactory
{
    #region Private Attributes
    // ThreadLocal ensures each worker thread manages its own isolated IWebDriver instance
    private static readonly ThreadLocal<IWebDriver?> LocalDriver = new(trackAllValues: true);
    #endregion

    #region Public Methods
    /// <summary>
    /// Gets the current worker thread's WebDriver instance, creating a new one if it does not exist.
    /// </summary>
    /// <returns>The active thread's IWebDriver instance.</returns>
    public static IWebDriver GetOrCreateDriver()
    {
        if (!LocalDriver.IsValueCreated || LocalDriver.Value == null)
        {
            LocalDriver.Value = CreateDriverInstance();
        }

        return LocalDriver.Value;
    }

    /// <summary>
    /// Clears cookies, local storage, and session storage to reset browser state between scenario runs.
    /// </summary>
    public static void ResetSession()
    {
        if (LocalDriver.IsValueCreated && LocalDriver.Value != null)
        {
            try
            {
                IWebDriver driver = LocalDriver.Value;
                driver.Manage().Cookies.DeleteAllCookies();

                if (driver is IJavaScriptExecutor js)
                {
                    js.ExecuteScript("window.localStorage.clear(); window.sessionStorage.clear();");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to reset browser session on Thread {Environment.CurrentManagedThreadId}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Quits and disposes all active WebDriver instances across all worker threads at the end of the test run.
    /// </summary>
    public static void QuitAllDrivers()
    {
        foreach (IWebDriver? driver in LocalDriver.Values)
        {
            if (driver != null)
            {
                try { driver.Quit(); } catch { }
                try { driver.Dispose(); } catch { }
            }
        }

        LocalDriver.Dispose();
    }
    #endregion

    #region Private Helper Methods
    /// <summary>
    /// Instantiates a new IWebDriver based on application configuration.
    /// </summary>
    private static IWebDriver CreateDriverInstance()
    {
        BrowserType browserType = ConfigManager.Browser;

        ChromiumOptions options = browserType switch
        {
            BrowserType.Chrome => new ChromeOptions(),
            BrowserType.Edge => new EdgeOptions(),
            _ => throw new ArgumentException($"Unsupported browser type: {browserType}")
        };

        if (ConfigManager.Headless)
        {
            options.AddArgument("--headless=new");
            options.AddArgument("--window-size=1920,1080");
        }

        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-dev-shm-usage");

        DisableExtensionsAndBanners(options);

        IWebDriver driver = browserType switch
        {
            BrowserType.Chrome => new ChromeDriver((ChromeOptions)options),
            BrowserType.Edge => new EdgeDriver((EdgeOptions)options),
            _ => throw new ArgumentException($"Unsupported browser type: {browserType}")
        };

        try
        {
            if (!ConfigManager.Headless)
            {
                driver.Manage().Window.Maximize();
            }
        }
        catch { }

        return driver;
    }

    /// <summary>
    /// Disables browser extensions, popups, infobars, and password management features.
    /// </summary>
    private static void DisableExtensionsAndBanners(ChromiumOptions options)
    {
        options.AddUserProfilePreference("credentials_enable_service", false);
        options.AddUserProfilePreference("profile.password_manager_enabled", false);
        options.AddUserProfilePreference("profile.password_manager_leak_detection", false);
        options.AddUserProfilePreference("safebrowsing.password_protection_warning_trigger", 0);
        options.AddArgument("--disable-features=PasswordLeakDetection");
        options.AddArgument("--disable-features=SafeBrowsingPasswordProtection");
        options.AddArgument("--disable-popup-blocking");
        options.AddArgument("--disable-infobars");
    }
    #endregion
}