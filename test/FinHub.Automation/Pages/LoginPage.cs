using FinHub.Automation.Core.Extensions;
using FinHub.Automation.Config;
using FinHub.Automation.Helpers;
using FinHub.Automation.Models;

namespace FinHub.Automation.Pages;

/// <summary>
/// A class that represents the login page.
/// </summary>
public class LoginPage : BasePage
{
    #region Public Properties
    // URL
    public override string PageUrl => $"{ConfigManager.BaseUrl}/login";

    // Registry
    public enum ValidationKey
    {
        SuccessfulLoginUrl
    }

    public readonly Dictionary<ValidationKey, string> Registry;
    #endregion

    #region Page Locators
    private readonly By _emailFieldLocator = By.Name("email");
    private readonly By _passwordFieldLocator = By.Name("password");
    private readonly By _signInButtonLocator = By.CssSelector("button[type='submit']");
    private readonly By _errorMessageContainerLocator = By.CssSelector(".auth-error-message");
    #endregion

    #region Page Elements
    private IWebElement EmailField => _driver.WaitAndFindElement(_emailFieldLocator);
    private IWebElement PasswordField => _driver.WaitAndFindElement(_passwordFieldLocator);
    private IWebElement SignInButton => _driver.WaitAndFindElement(_signInButtonLocator);
    private IWebElement ErrorMessageContainer => _driver.WaitAndFindElement(_errorMessageContainerLocator);
    #endregion

    #region Constructor
    public LoginPage(IWebDriver driver) : base(driver)
    {
        Registry = new()
        {
            { ValidationKey.SuccessfulLoginUrl, $"{ConfigManager.BaseUrl}/overview" }
        };
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Logs in using the credentials associated with the specified role.
    /// </summary>
    /// <param name="role">The role for which to use credentials.</param>
    public void LoginAsRole(string role)
    {
        RoleCredentials credentials = CredentialManager.GetCredentials(role);

        LoginWithCredentials(credentials.Email, credentials.Password);
    }

    /// <summary>
    /// Logs in using the specified email and password.
    /// </summary>
    /// <param name="email">The email to use for login.</param>
    /// <param name="password">The password to use for login.</param>
    public void LoginWithCredentials(string email, string password)
    {
        EmailField.SendKeys(email);
        PasswordField.SendKeys(password);
        SignInButton.Click();
    }

    /// <summary>
    /// Retrieves the error message displayed on the login page, if any.
    /// </summary>
    /// <returns></returns>
    public string GetErrorMessage() => ErrorMessageContainer.Text;

    /// <summary>
    /// Waits until the login succeeds or an error is displayed, then reports login failures immediately.
    /// </summary>
    public void WaitUntilLoginIsSuccessful()
    {
        string expectedUrl = Registry[ValidationKey.SuccessfulLoginUrl];

        _wait.Until(driver =>
            driver.Url.Contains(expectedUrl, StringComparison.OrdinalIgnoreCase) ||
            driver.FindElements(_errorMessageContainerLocator).Any(element => element.Displayed));

        if (!_driver.Url.Contains(expectedUrl, StringComparison.OrdinalIgnoreCase))
        {
            string? errorMessage = _driver.FindElements(_errorMessageContainerLocator)
                .FirstOrDefault(element => element.Displayed)?.Text;

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(errorMessage)
                    ? $"Login did not navigate to the expected URL '{expectedUrl}'. Current URL: '{_driver.Url}'."
                    : $"Login failed: {errorMessage}");
        }
    }
    #endregion
}
