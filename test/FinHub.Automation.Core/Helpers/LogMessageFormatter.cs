namespace FinHub.Automation.Core.Helpers;

/// <summary>
/// A class that includes helper methods for formatting HTML log messages and exceptions.
/// </summary>
public class LogMessageFormatter
{
    #region Public Methods
    /// <summary>
    /// Formats a standard informational log message with a blue color highlight.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <returns>The HTML formatted log message.</returns>
    public static string FormatLogMessage(string message) => $"<span style='color:#3498db;font-weight:bold;'>{message}</span>";

    /// <summary>
    /// Formats a success message with a green color highlight.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <returns>The HTML formatted pass message.</returns>
    public static string FormatPassMessage(string message) => $"<span style='color:#2ecc71;font-weight:bold;'>{message}</span>";

    /// <summary>
    /// Formats an error message with a red color highlight.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <returns>The HTML formatted error message.</returns>
    public static string FormatErrorMessage(string message) => $"<span style='color:#e74c3c;font-weight:bold;'>{message}</span>";

    /// <summary>
    /// Formats an exception into a user-friendly error message and the 
    /// full exception details in a collapsible section for debugging purposes.
    /// </summary>
    /// <param name="ex">The exception to format.</param>
    /// <returns>The formatted error message with details.</returns>
    public static string FormatExceptionMessage(Exception ex)
    {
        string friendlyMessage = GetFriendlyErrorMessage(ex);
        string message = ex.Message;
        return $"<br><span style='color:#c0392b;font-weight:bold;'>[ERROR] {friendlyMessage}</span><br><details><summary style='cursor:pointer;color:#888;font-weight:bold;'>[DETAILS]</summary><pre style='font-size:12px;color:#666;whitespace:pre-wrap;'>{message}</pre></details>";
    }
    #endregion

    #region Private Helper Methods
    /// <summary>
    /// Returns a user-friendly error message that describes the specified exception.
    /// </summary>
    /// <param name="ex">The exception for which to generate a user-friendly error message.</param>
    /// <returns>The user-friendly error message describing the exception.</returns>
    private static string GetFriendlyErrorMessage(Exception ex)
    {
        return ex.GetType().Name switch
        {
            "AssertionException" => $"An assertion failed: {ex.Message}",
            "AssertFailedException" => $"An assertion failed: {ex.Message}",
            "ElementClickInterceptedException" => "Element was found but could not be clicked. Another element may be covering it (e.g., a modal or overlay) or it may not be interactable.",
            "ElementNotInteractableException" => "Element was found but is not currently interactable. It may be hidden, disabled, or covered by another element.",
            "InvalidOperationException" => "The operation is not valid in the current state. This may occur if the WebDriver session has ended or if an action is attempted on a closed browser.",
            "InvalidSelectorException" => "Element locator is invalid. Check the syntax of the locator (e.g., XPath, CSS selector).",
            "NoSuchFrameException" => "Frame was not found. Check if the frame exists and if you have switched to the correct context.",
            "NoSuchElementException" => "Element was not found on the page. The locator may be incorrect or the element has not been loaded yet.",
            "NoSuchWindowException" => "Window or tab was not found. It might have been closed unexpectedly.",
            "StaleElementReferenceException" => "Element was found but is no longer attached to the DOM. The page may have been refreshed or updated since the element was located.",
            "UnhandledAlertException" => "An unexpected browser alert/popup was present. Handle the alert before interacting with the page.",
            "WebDriverTimeoutException" => "Timed out waiting for an element or page to load.",
            _ => ex.Message,
        };
    }
    #endregion
}