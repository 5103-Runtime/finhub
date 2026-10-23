using FinHub.Automation.Core.Extensions;
using FinHub.Automation.Pages;

namespace FinHub.Automation.StepDefinitions;

/// <summary>
/// The base class for step definitions in the FinHub automation framework, providing common functionality and access to the WebDriver instance.
/// </summary>
public class BaseStepDefinitions
{
    #region Private & Protected Attributes
    // Driver & Contexts
    private IWebDriver? _driver;
    private LoginPage? _loginPage;

    protected readonly ScenarioContext _scenarioContext;
    protected readonly FeatureContext _featureContext;
    #endregion

    #region Public Properties
    /// <summary>
    /// Lazy initialization of the WebDriver instance from the ScenarioContext.
    /// </summary>
    public IWebDriver Driver
    {
        get
        {
            _driver ??= _scenarioContext.GetDriver();
            return _driver;
        }
    }

    /// <summary>
    /// Lazy initialization of the LoginPage instance.
    /// </summary>
    public LoginPage LoginPage
    {
        get
        {
            _loginPage ??= new(Driver);
            return _loginPage;
        }
    }
    #endregion

    #region Constructor
    public BaseStepDefinitions(ScenarioContext scenarioContext, FeatureContext featureContext)
    {
        _scenarioContext = scenarioContext ?? throw new ArgumentNullException(nameof(scenarioContext));
        _featureContext = featureContext ?? throw new ArgumentNullException(nameof(featureContext));
    }
    #endregion
}
