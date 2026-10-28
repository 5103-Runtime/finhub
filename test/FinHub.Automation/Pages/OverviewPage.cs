using FinHub.Automation.Config;
using FinHub.Automation.Core.Extensions;

namespace FinHub.Automation.Pages;

/// <summary>
/// A class that represents the overview page.
/// </summary>
public class OverviewPage : BasePage
{
    #region Public Properties
    // URL
    public override string PageUrl => $"{ConfigManager.BaseUrl}/overview";
    #endregion

    #region Page Locators
    private readonly By _avatarLocator = By.CssSelector(".dashboard-avatar");
    private readonly By _welcomeMessageLocator = By.CssSelector(".overview-title");
    #endregion

    #region Page Elements
    private IWebElement Avatar => _driver.WaitAndFindElement(_avatarLocator);
    private IWebElement WelcomeMessage => _driver.WaitAndFindElement(_welcomeMessageLocator);
    #endregion

    #region Constructor
    public OverviewPage(IWebDriver driver) : base(driver)
    {
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Waits until the overview page is fully loaded by checking the visibility of the avatar and welcome message elements.
    /// Avatar represents the navbar, whereas the welcome message represents the main content of the page.
    /// Both elements must be displayed for the page to be considered fully loaded.
    /// </summary>
    public void WaitUntilPageIsLoaded()
    {
        // Wait until the avatar and welcome message elements are loaded on the page
        _ = Avatar;
        _ = WelcomeMessage;
    }

    /// <summary>
    /// Checks if the user is logged in by verifying the visibility of the avatar and welcome message elements.
    /// </summary>
    /// <returns>True if the user is logged in, false otherwise.</returns>
    public bool IsUserLoggedIn()
    {
        return _driver.IsElementDisplayed(_avatarLocator) &&
               _driver.IsElementDisplayed(_welcomeMessageLocator);
    }
    #endregion
}
