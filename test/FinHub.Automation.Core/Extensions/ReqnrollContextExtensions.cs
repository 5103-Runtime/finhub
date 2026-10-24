using OpenQA.Selenium;
using Reqnroll;

namespace FinHub.Automation.Core.Extensions;

/// <summary>
/// A class that contains extension methods for the ReqnrollContext class.
/// </summary>
public static class ReqnrollContextExtensions
{
    #region Private Attributes
    // Reqnroll Context Keys
    private const string WebDriverKey = "WebDriver";
    #endregion

    #region Public Methods
    /// <summary>
    /// Gets the WebDriver instance from the current scenario context.
    /// </summary>
    /// <remarks>Should be used by step definitions to retrieve the driver instance.</remarks>
    /// <param name="reqnrollContext">The Reqnroll context.</param>
    /// <returns>The WebDriver instance.</returns>
    /// <exception cref="InvalidOperationException">Throws if the WebDriver instance is not found in the scenario context.</exception>
    public static IWebDriver GetDriver(this ReqnrollContext reqnrollContext)
    {
        if (reqnrollContext.TryGetValue(WebDriverKey, out IWebDriver driver) && driver != null)
        {
            return driver;
        }

        throw new InvalidOperationException("WebDriver instance not found in ScenarioContext. Ensure BeforeScenario hook has been executed.");
    }

    /// <summary>
    /// Sets the WebDriver instance in the current scenario context.
    /// </summary>
    /// <param name="reqnrollContext">The Reqnroll context.</param>
    /// <param name="driver">The WebDriver instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if the WebDriver instance is null.</exception>
    public static void SetDriver(this ReqnrollContext reqnrollContext, IWebDriver driver)
    {
        reqnrollContext[WebDriverKey] = driver ?? throw new ArgumentNullException(nameof(driver), "WebDriver instance cannot be null.");
    }

    /// <summary>
    /// Gets a value of type T from the ReqnrollContext using the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="reqnrollContext">The Reqnroll context.</param>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <returns>The value of type T.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the value is not found or is not of the expected type.</exception>
    public static T GetValue<T>(this ReqnrollContext reqnrollContext, string key)
    {
        if (reqnrollContext.TryGetValue(key, out object value) && value is T typedValue)
        {
            return typedValue;
        }

        throw new InvalidOperationException($"Value for key '{key}' not found or is not of type {typeof(T).Name}.");
    }

    /// <summary>
    /// Sets a value of type T in the ReqnrollContext using the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the value to set.</typeparam>
    /// <param name="reqnrollContext">The Reqnroll context.</param>
    /// <param name="key">The key of the value to set.</param>
    /// <param name="value">The value to set.</param>
    /// <exception cref="ArgumentNullException">Thrown if the value is null.</exception>
    public static void SetValue<T>(this ReqnrollContext reqnrollContext, string key, T value)
    {
        reqnrollContext[key] = value ?? throw new ArgumentNullException(nameof(value), $"Value for key '{key}' cannot be null.");
    }
    #endregion
}