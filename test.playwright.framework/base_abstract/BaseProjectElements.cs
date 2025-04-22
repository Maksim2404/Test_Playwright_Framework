using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.constants;
using test.playwright.framework.pages;
using test.playwright.framework.utils;

namespace test.playwright.framework.base_abstract;

public class BaseProjectElements(IPage page) : AssertUtils(page)
{
    private ILocator SaveButton => Page.Locator("button[data-test='saveButton']");
    private ILocator NextPageButton => Page.Locator("//button[@aria-label='Next page']");
    private ILocator PreviousPageButton => Page.Locator("//button[@aria-label='Previous page']");
    private ILocator ClickCustomButton(string buttonText) => Page.Locator($"//button[text()='{buttonText}']");

    protected internal readonly Dictionary<string, string> LanguageCodes = new()
    {
        { "Albanian", "SQ" },
        { "Arabic", "AR" },
        { "Azeri", "AZ" },
        { "English", "EN" }
    };
    
    protected async Task<T> ClickSaveButton<T>() where T : BaseProjectElements
    {
        await Click(SaveButton);
        return (T)Activator.CreateInstance(typeof(T), Page)!;
    }

    private async Task<T> ClickButton<T>(ILocator buttonSelector) where T : BaseProjectElements
    {
        await WaitForLocatorToExistAsync(buttonSelector);
        await Click(buttonSelector);
        return (T)Activator.CreateInstance(typeof(T), Page)!;
    }

    private async Task<T> InputValueTo<T>(ILocator inputSelector, string text) where T : BaseProjectElements
    {
        await WaitForLocatorToExistAsync(inputSelector);
        await Input(inputSelector, text);
        return (T)Activator.CreateInstance(typeof(T), Page)!;
    }

    public async Task UploadFileAsync(string selectFilesField)
    {
        try
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), TestDataConstants.FilesToUploadDirectory,
                TestDataConstants.FileNameToUpload);

            Log.Information($"Attempting to upload file: {filePath} using selector: {selectFilesField}");
            await Page.SetInputFilesAsync(selectFilesField, filePath);
            Log.Information($"File upload attempted for: {TestDataConstants.FileNameToUpload}");
        }
        catch (Exception ex)
        {
            Log.Error(
                $"An error occurred while attempting to upload file: {TestDataConstants.FileNameToUpload}. Error: {ex.Message}");
            throw;
        }
    }

    protected async Task<T> ClickUploadFileSubmitButton<T>(int numberOfFiles) where T : BaseProjectElements
    {
        var buttonText = numberOfFiles == 1 ? "Upload 1 file" : $"Upload {numberOfFiles} files";

        await Click(ClickCustomButton(buttonText));
        return (T)Activator.CreateInstance(typeof(T), Page)!;
    }

    private static async Task WaitForButtonToEnableAsync(ILocator locator, int timeoutMs = 10000)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            if (await locator.IsEnabledAsync())
            {
                Log.Information("Button is now enabled!");
                return;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("Button did not become enabled in time.");
    }

    public async Task<bool> IsLocatorPresent(ILocator locator, int shortTimeoutMs = 1000)
    {
        try
        {
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = shortTimeoutMs
            });

            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (PlaywrightException ex)
        {
            Log.Error($"Playwright error checking IsLocatorPresent: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> TryNavigatePage(PageNavigationDirection direction)
    {
        var buttonLocator = direction == PageNavigationDirection.Next
            ? NextPageButton
            : PreviousPageButton;

        await WaitForLocatorToExistAsync(buttonLocator);
        await WaitForButtonToEnableAsync(buttonLocator);

        var ariaDisabled = await buttonLocator.GetAttributeAsync("aria-disabled");
        var isDisabled = ariaDisabled?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

        if (isDisabled)
        {
            Log.Warning($"{direction} Page button is disabled; cannot navigate.");
            return false;
        }

        await Click(buttonLocator);
        Log.Information($"{direction} page button clicked successfully.");
        return true;
    }

    protected async Task<bool> ValidateSomeDetails(string name, string workType, string customer,
        string? language = null)
    {
        var expectedDetails = new Dictionary<string, string>
        {
            { "Name", name },
            { "Work Type", workType },
            { "Customer", customer }
        };

        if (!string.IsNullOrEmpty(language))
        {
            expectedDetails.Add("Language", language);
        }

        foreach (var detail in expectedDetails)
        {
            var expectedValue = detail.Value;

            if (detail.Key == "Language" && language != null && LanguageCodes.ContainsKey(language))
            {
                expectedValue = $"{LanguageCodes[language]} {language}";
            }

            var xpath = $"//tr[td[1][normalize-space(text())='{detail.Key}']]/td[2]";
            var valueLocator = Page.Locator(xpath);
            await WaitForLocatorToExistAsync(valueLocator);
            var actualValue = await valueLocator.TextContentAsync() ?? string.Empty;

            if (actualValue.Trim().Equals(expectedValue)) continue;
            Log.Warning($"Mismatch found: {detail.Key} - Expected: {expectedValue}, Actual: {actualValue}");
        }

        Log.Information($"All template properties verified successfully for '{name}'.");
        return true;
    }
    
    protected async Task<UiActionState> GetEllipsisMenuItemStateAsync(ILocator item, string disabledClass)
    {
        if (!await WaitForLocatorToExistAsync(item))
        {
            Log.Warning("Button not found.");
            return UiActionState.Missing;
        }

        var isDisabled = await item.EvaluateAsync<bool>(
            $"e => e.classList.contains('{disabledClass}')");

        var state = isDisabled ? UiActionState.Disabled : UiActionState.Enabled;
        Log.Information($"Button state: {state}");
        return state;
    }

    public static int ParseEntityCount(string text, string entityName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Log.Warning($"Input text is null or empty for parsing {entityName} count.");
            return 0;
        }

        var pattern = $@"{entityName}\s*\((\d+)\)";
        var match = Regex.Match(text, pattern);

        if (match.Success)
        {
            Log.Information($"Successfully parsed {entityName} count: {match.Groups[1].Value} from text: '{text}'.");
            return int.Parse(match.Groups[1].Value);
        }
        else
        {
            Log.Warning($"Failed to parse {entityName} count from text: '{text}'.");
            return 0;
        }
    }
}