using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using FinHub.Automation.Core.Config;
using System.Collections.ObjectModel;

namespace FinHub.Automation.Core.Extensions;

/// <summary>
/// A class that contains extension methods for the IWebDriver interface.
/// </summary>
public static class WebDriverExtensions
{
    /// <summary>
    /// Waits for an element to be present in the DOM and visible on the page before locating it.
    /// </summary>
    /// <param name="driver">The IWebDriver instance.</param>
    /// <param name="locator">The locator of the element.</param>
    /// <returns>The located element.</returns>
    /// <exception cref="WebDriverTimeoutException">Throws if the element is not found or not visible within the timeout period.</exception>
    public static IWebElement WaitAndFindElement(this IWebDriver driver, By locator)
    {
        WebDriverWait wait = new(driver, TimeSpan.FromSeconds(ConfigProvider.DefaultTimeout));
        // Ignore common transient exceptions that may occur while waiting for the element to be present and visible.
        wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException));

        try
        {
            return wait.Until(driver =>
            {
                ReadOnlyCollection<IWebElement> elements = driver.FindElements(locator);
                // Find the first element in the collection that is currently visible
                IWebElement? visibleElement = elements.FirstOrDefault(e => e.Displayed);

                return visibleElement;
            }) ?? throw new WebDriverTimeoutException($"Element with locator {locator} was not found or not visible within the timeout period.");
        }
        catch (WebDriverTimeoutException ex)
        {
            // Re-throw with a more descriptive message for ExtentReports
            throw new WebDriverTimeoutException($"Timed out after {ConfigProvider.DefaultTimeout} seconds waiting for element with locator {locator}.", ex);
        }
    }

    /// <summary>
    /// Checks whether an element matching the locator is present and visible without waiting.
    /// </summary>
    /// <param name="driver">The IWebDriver instance.</param>
    /// <param name="locator">The locator of the element.</param>
    /// <returns>True if at least one matching element is visible; otherwise, false.</returns>
    public static bool IsElementDisplayed(this IWebDriver driver, By locator) =>
        driver.FindElements(locator).Any(element => element.Displayed);
}