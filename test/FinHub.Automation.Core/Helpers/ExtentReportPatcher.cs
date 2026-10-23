using System.Text;
using System.Text.RegularExpressions;

namespace FinHub.Automation.Core.Helpers;

/// <summary>
/// A class that patches the Extent Report HTML file to correct dashboard summary card counts
/// to reflect the scenario counts instead of feature counts, since the Extent Report calculates
/// the dashboard counts based on the number of test nodes(features) created.
/// </summary>
public static class ExtentReportPatcher
{
    /// <summary>
    /// Patches the Extent Report HTML file at the specified path to correctly reflect the scenario counts in the dashboard summary cards.
    /// </summary>
    /// <param name="reportPath">The path to the Extent Report HTML file.</param>
    /// <exception cref="ArgumentNullException">Thrown if the report path is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the Extent Report file is not found.</exception>
    public static void Patch(string reportPath)
    {
        try
        {
            // Validate the report path and ensure the file exists
            if (string.IsNullOrEmpty(reportPath))
            {
                throw new ArgumentNullException(nameof(reportPath));
            }

            if (!File.Exists(reportPath))
            {
                throw new FileNotFoundException($"The Extent Report file was not found: {reportPath}");
            }

            // Read the HTML content of the Extent Report
            string html = File.ReadAllText(reportPath);

            // 1. Rename the test labels in the card footers to "features" instead of "tests" for better clarity
            html = ReplaceWordInCard(html, "parent-analysis", "tests passed", "features passed");
            html = ReplaceWordInCard(html, "parent-analysis", "tests failed", "features failed");

            // 2. Rename the steps labels in the child-analysis card footer to "scenarios" instead of "steps" for better clarity
            html = ReplaceWordInCard(html, "child-analysis", "steps passed", "scenarios passed");
            html = ReplaceWordInCard(html, "child-analysis", "steps failed", "scenarios failed");

            // 3. Replace the card titles from "Tests" and "Steps" to "Features" and "Scenarios" respectively for better clarity
            html = ReplaceCardTitle(html, "Tests", "Features");
            html = ReplaceCardTitle(html, "Steps", "Scenarios");

            var (passed, failed, skipped) = ExtractCountsFromStepsCard(html);
            Console.WriteLine($"[LOG] Extracted counts - Passed: {passed}, Failed: {failed}, Skipped: {skipped}");

            // 4. Replace the test counts in the summary cards with the actual scenario counts
            html = ReplaceSummaryCard(html, "Tests Passed", passed.ToString());
            html = ReplaceSummaryCard(html, "Tests Failed", failed.ToString());

            // Write the modified HTML back to the report file
            File.WriteAllText(reportPath, html, Encoding.UTF8);
            Console.WriteLine($"[LOG] Report patched successfully: {passed} passed, {failed} failed, {skipped} skipped");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Patch failed: {ex.Message}");
        }
    }

    #region Private Helper Methods
    /// <summary>
    /// Extracts the passed, failed, and skipped counts from the Steps card footer (canvas id="child-analysis") 
    /// in the Extent Report HTML. This cards reflects child-node counts (scenarios), maing it the relevant source for accurate scenario counts.
    /// </summary>
    /// <remarks>
    /// Targets this structure:
    ///     <b>2</b> steps passed OR <b>2</b> scenarios passed
    ///     <b>0</b> steps failed OR <b>0</b> scenarios failed
    ///     <b>0</b> skipped
    /// </remarks>
    /// <param name="html">The HTML content of the Extent Report.</param>
    /// <returns>A tuple of passed, failed, and skipped counts.</returns>
    private static (int passed, int failed, int skipped) ExtractCountsFromStepsCard(string html)
    {
        // Isolate the child-analysis card block first to avoid false matches in other cards
        Match cardMatch = Regex.Match(
            html,
            @"id='child-analysis'[\s\S]*?card-footer[\s\S]*?</div>\s*</div>\s*</div>\s*</div>",
            RegexOptions.Singleline
        );

        if (!cardMatch.Success)
        {
            Console.WriteLine("[ERROR] ExtractCountsFromStepsCard: child-analysis card not found.");
            return (0, 0, 0);
        }

        string cardBlock = cardMatch.Value;

        // Extract passed: <b>N</b> steps passed OR <b>N</b> scenarios passed
        int passed = ExtractCount(cardBlock, @"<b[^>]*>(\d+)</b>\s*(?:steps|scenarios)\s*passed");

        // Extract failed: <b>N</b> steps failed OR <b>N</b> scenarios failed
        int failed = ExtractCount(cardBlock, @"<b[^>]*>(\d+)</b>\s*(?:steps|scenarios)\s*failed");

        // Extract skipped: <b>N</b> skipped
        int skipped = ExtractCount(cardBlock, @"<b[^>]*>(\d+)</b>\s*skipped");

        return (passed, failed, skipped);
    }

    /// <summary>
    /// Extracts the first integer captured by group 1 of the given pattern within the text.
    /// </summary>
    /// <remarks>Returns 0 if no match is found.</remarks>
    /// <param name="text">The text to search within.</param>
    /// <param name="pattern">The pattern to match.</param>
    /// <returns>The extracted count or 0 if not found.</returns>
    private static int ExtractCount(string text, string pattern)
    {
        Match match = Regex.Match(text, pattern, RegexOptions.Singleline);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int count))
        {
            return count;
        }


        Console.WriteLine($"[ERROR] ExtractBoldNUmber: Pattern not found or invalid number for pattern: {pattern}");
        return 0;
    }

    /// <summary>
    /// Replaces the <h3> value that immediately follows the summary card <p> label.
    /// </summary>
    /// <remarks>
    /// Targets this structure:
    ///     <p class="m-b-0 text-pass">Tests Passed</p>
    ///     <h3>2</h3>
    ///     
    ///     <p class="m-b-0 text-fail">Tests Failed</p>
    ///     <h3>0</h3>
    ///     
    /// This class on the <p> tag varies by status (text-pass, text-fail, text-skip),
    /// so matches any class attribute value to stay robust across all card types.
    /// </remarks>
    /// <param name="html">The HTML content of the Extent Report.</param>
    /// <param name="label">The label of the summary card to replace.</param>
    /// <param name="newValue">The new value for the summary card.</param>
    /// <returns>The updated HTML content.</returns>
    private static string ReplaceSummaryCard(string html, string label, string newValue)
    {
        string pattern = $@"(<p[^>]*>{Regex.Escape(label)}</p>\s*<h3>)\d+(</h3>)";
        string result = Regex.Replace(
            html,
            pattern,
            $"${{1}}{newValue}${{2}}",
            RegexOptions.Singleline
        );

        if (result == html)
        {
            Console.WriteLine($"[ERROR] ReplaceSummaryCard: '{label}' card not found - Check the HTML structure of the Extent Report.");
        }

        return result;
    }

    /// <summary>
    /// Replaces a specific word/phrase inside a card identified by its canvas ID.
    /// </summary>
    /// <remarks>
    /// Used to rename:
    ///     - "tests passed" to "features passed"
    ///     - "steps passed" to "scenarios passed"
    /// </remarks>
    /// <param name="html">The HTML content of the Extent Report.</param>
    /// <param name="cardCanvasId">The canvas ID of the card to modify.</param>
    /// <param name="oldWord">The word/phrase to replace.</param>
    /// <param name="newWord">The new word/phrase.</param>
    /// <returns>The updated HTML content.</returns>
    private static string ReplaceWordInCard(
        string html,
        string cardCanvasId,
        string oldWord,
        string newWord)
    {
        string cardPattern = $@"(id='{Regex.Escape(cardCanvasId)}'[\s\S]*?card-footer[\s\S]*?)(</div>\s*</div>\s*</div>\s*</div>)";
        string result = Regex.Replace(
            html,
            cardPattern,
            match =>
            {
                string cardContent = match.Groups[1].Value.Replace(oldWord, newWord);
                return cardContent + match.Groups[2].Value;
            },
            RegexOptions.Singleline
        );

        if (result == html)
        {
            Console.WriteLine($"[ERROR] ReplaceWordInCard: Card with canvas ID '{cardCanvasId}' not found - Check the HTML structure of the Extent Report.");
        }

        return result;
    }

    /// <summary>
    /// Renames a card title in the report header.
    /// </summary>
    /// <remarks>
    /// Targets this structure:
    ///     <h6 class="card-title">Tests</h6>
    ///     <h6 class="card-title">Steps</h6>
    /// </remarks>
    /// <param name="html">The HTML content of the Extent Report.</param>
    /// <param name="oldTitle">The old title of the card.</param>
    /// <param name="newTitle">The new title of the card.</param>
    /// <returns>The updated HTML content.</returns>
    private static string ReplaceCardTitle(string html, string oldTitle, string newTitle)
    {
        string pattern = $@"(<h6[^>]*class=""card-title""[^>]*>){Regex.Escape(oldTitle)}(</h6>)";
        string result = Regex.Replace(
            html,
            pattern,
            $"${{1}}{newTitle}${{2}}"
        );

        if (result == html)
        {
            Console.WriteLine($"[ERROR] ReplaceCardTitle: Card with title '{oldTitle}' not found - Check the HTML structure of the Extent Report.");
        }

        return result;
    }
    #endregion
}