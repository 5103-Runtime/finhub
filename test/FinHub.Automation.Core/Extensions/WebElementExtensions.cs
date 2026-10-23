using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;

namespace FinHub.Automation.Core.Extensions;

/// <summary>
/// A class that contains extension methods for the IWebElement interface.
/// </summary>
public static class WebElementExtensions
{
    /// <summary>
    /// Hovers over the specified IWebElement using Selenium Actions.
    /// </summary>
    /// <param name="element">The IWebElement to hover over.</param>
    public static void Hover(this IWebElement element)
    {
        // Extract driver reference directly from the element
        IWebDriver driver = ((IWrapsDriver)element).WrappedDriver;

        Actions actions = new(driver);
        actions.MoveToElement(element).Perform();
    }
}