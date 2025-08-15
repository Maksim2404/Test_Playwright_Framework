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
    private ILocator FileInputSelector => Page.Locator("//div[@class='uppy-Dashboard-AddFiles']//input[1]");
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

    protected static string GetCurrentDate()
    {
        return DateTime.Now.ToString("MMM d, yyyy");
    }

    public async Task UploadFileAsync(params string[] fileNames)
    {
        try
        {
            var filePaths = fileNames.Select(fileName =>
                    Path.Combine(Directory.GetCurrentDirectory(), TestDataConstants.FilesToUploadDirectory, fileName))
                .ToArray();

            Log.Information(
                $"Attempting to upload files: {string.Join(", ", filePaths)} using selector: {FileInputSelector}");

            await FileInputSelector.SetInputFilesAsync(filePaths);
            Log.Information($"File upload attempted for: {string.Join(", ", fileNames)}");
        }
        catch (Exception ex)
        {
            Log.Error(
                $"An error occurred while attempting to upload files: {string.Join(", ", fileNames)}. Error: {ex.Message}");
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

    protected async Task<bool> ValidateSomeDetails(string templateName, string workType, string customer,
        string? language = null)
    {
        var expectedDetails = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            { "Name", templateName },
            { "Work Type", workType },
            { "Customer", customer }
        };

        if (!string.IsNullOrEmpty(language) && LanguageCodes.ContainsKey(language))
        {
            expectedDetails.Add("Language", $"{LanguageCodes[language]} {language}");
        }

        return await VerifyTableDetailsAsync(Page, expectedDetails, templateName);
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