
using AventStack.ExtentReports.Gherkin.Model;
using FinHub.Automation.Helpers;
using FinHub.Automation.Pages;

namespace FinHub.Automation.StepDefinitions;

/// <summary>
/// A class that defines the step definitions for the user authentication feature.
/// </summary>
[Binding]
public class UserAuthenticationStepDefinitions : BaseStepDefinitions
{
    #region Constructor
    public UserAuthenticationStepDefinitions(ScenarioContext scenarioContext, FeatureContext featureContext) : base(scenarioContext, featureContext)
    {
    }
    #endregion

    #region Given Steps
    [Given(@"I am on the login page")]
    public void GivenIAmOnTheLoginPage()
    {
        LoginPage.Navigate();
    }
    #endregion

    #region When Steps
    [When(@"I log in as {}")]
    public void WhenILogInAsUser(string accountType)
    {
        string accountKey = accountType.Trim().ToLowerInvariant();

        LoginPage.LoginAsRole(accountKey);
    }
    #endregion

    #region Then Steps
    [Then(@"I should be logged in successfully")]
    public void ThenIShouldBeLoggedInSuccessfully()
    {
        LoginPage.WaitUntilLoginIsSuccessful();
        Assert.Contains(LoginPage.Registry[LoginPage.ValidationKey.SuccessfulLoginUrl], Driver.Url, "User was not logged in successfully.");
    }
    #endregion
}
