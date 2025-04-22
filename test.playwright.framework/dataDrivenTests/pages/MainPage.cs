using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.base_abstract;

namespace test.playwright.framework.dataDrivenTests.pages;

public class MainPage(IPage page) : BaseProjectElements(page)
{
    /// <summary>
    /// Navigates to a specified section (e.g., Web Application, Mobile Application).
    /// </summary>
    /// <param name="sectionName">The name of the section to navigate to.</param>
    public async Task NavigateToSectionAsync(string sectionName)
    {
        try
        {
            var sectionLocator = Page.Locator($"//nav//a[text()='{sectionName}']");
            await WaitForLocatorToExistAsync(sectionLocator);
            await Click(sectionLocator);
            await WaitForNetworkIdle();
            Log.Information($"Navigated to section: {sectionName}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to navigate to section '{sectionName}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Verifies if a task exists in the specified column with the correct tags.
    /// </summary>
    /// <param name="taskTitle">The title of the task.</param>
    /// <param name="columnName">The name of the column (e.g., To Do, In Progress).</param>
    /// <param name="tags">Array of tags to verify for the task.</param>
    /// <returns>True if the task exists with the correct tags; otherwise, false.</returns>
    public async Task<bool> VerifyTaskInColumnAsync(string taskTitle, string columnName, string[] tags)
    {
        try
        {
            var columnSelector =
                $"//div[contains(@class, 'column')]//h2[text()='{columnName}']/../div[contains(@class, 'task-list')]";
            var taskCardSelector =
                Page.Locator($"{columnSelector}//div[contains(@class, 'task') and .//text()='{taskTitle}']");

            if (!await WaitForLocatorToExistAsync(taskCardSelector))
            {
                Log.Warning($"Task '{taskTitle}' not found in column '{columnName}'.");
                return false;
            }

            foreach (var tag in tags)
            {
                var tagSelector = Page.Locator($"{taskCardSelector}//span[contains(@class, 'tag') and text()='{tag}']");
                if (!await WaitForLocatorToExistAsync(tagSelector))
                {
                    Log.Warning($"Tag '{tag}' not found for task '{taskTitle}' in column '{columnName}'.");
                    return false;
                }
            }

            Log.Information(
                $"Task '{taskTitle}' verified successfully in column '{columnName}' with tags '{string.Join(", ", tags)}'.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Error verifying task '{taskTitle}' in column '{columnName}': {ex.Message}");
            return false;
        }
    }
}