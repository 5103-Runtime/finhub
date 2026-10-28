using OpenQA.Selenium.Support.UI;
using FinHub.Automation.Config;

namespace FinHub.Automation.Pages;

/// <summary>
/// A class that represents the base page object model for the web application under test, 
/// providing common functionality and properties for all page objects.
/// </summary>
public abstract class BasePage
{
    #region Protected Attributes
    protected readonly IWebDriver _driver;
    protected readonly WebDriverWait _wait;
    #endregion

    #region Public Properties
    public abstract string PageUrl { get; }
    #endregion

    #region Constructor
    public BasePage(IWebDriver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _wait = new(_driver, TimeSpan.FromSeconds(ConfigManager.DefaultTimeout));
        _wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException));
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Navigates to the page's URL.
    /// </summary>
    /// <remarks>
    /// This method navigates to the URL specified by the <see cref="PageUrl"/> 
    /// property, which must be overridden in derived classes.
    /// </remarks>
    public void Navigate() => _driver.Navigate().GoToUrl(PageUrl);

    /// <summary>
    /// Navigates to the specified URL.
    /// </summary>
    /// <param name="url">The URL to navigate to.</param>
    public void Navigate(string url) => _driver.Navigate().GoToUrl(url);

    /// <summary>
    /// Waits for the current URL to stabilize, meaning it remains the same for a short duration, indicating that the page has finished loading, navigating, or redirecting.
    /// </summary>
    /// <returns>The stabilized URL.</returns>
    public string WaitForUrlToStabilize()
    {
        string previousUrl = "";
        return _wait.Until(driver =>
        {
            string currentUrl = driver.Url;
            // If the current URL is the same as the previous URL, it means the page has stabilized, hence return the current URL
            // Otherwise, update the previous URL and continue waiting
            if (!string.IsNullOrEmpty(currentUrl) && currentUrl.Equals(previousUrl, StringComparison.OrdinalIgnoreCase))
            {
                // Sleep for a short duration to ensure the page has fully stabilized
                Thread.Sleep(500);
                if (currentUrl == previousUrl)
                {
                    return currentUrl;
                }
            }

            previousUrl = currentUrl;
            return null;
        });
    }
    #endregion

    #region Protected Helper Methods
    /// <summary>
    /// Scrolls the specified element into view using JavaScript, ensuring it is visible on the screen before interacting with it.
    /// </summary>
    /// <param name="element">The element to scroll into view.</param>
    protected void ScrollIntoView(IWebElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({ behavior: 'instant', block: 'center' });", element);
    }
    #endregion
}
