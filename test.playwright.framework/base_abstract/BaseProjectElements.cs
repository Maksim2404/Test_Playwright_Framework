using System.Diagnostics;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.fixtures.constants;
using test.playwright.framework.pages.enums;
using test.playwright.framework.pages.mail;
using test.playwright.framework.utils;

namespace test.playwright.framework.base_abstract;

public class BaseProjectElements(IPage page) : AssertUtils(page)
{
    private ILocator SaveButton => Page.Locator("button[data-test='saveButton']");
    private ILocator NextPageButton => Page.Locator("//button[@aria-label='Next page']");
    private ILocator PreviousPageButton => Page.Locator("//button[@aria-label='Previous page']");
    private ILocator FileInputSelector => Page.Locator("//div[@class='uppy-Dashboard-AddFiles']//input[1]");
    private ILocator MailpitHeaderTitle => Page.Locator("//a[@aria-current='page']/span[text()='Mailpit']");
    private const string MailpitUrl = "https://mailpit.com/";
    private ILocator ClickCustomButton(string buttonText) => Page.Locator($"//button[text()='{buttonText}']");

    private readonly Dictionary<string, string> _languageCodes = new()
    {
        { "Albanian", "SQ" },
        { "Arabic", "AR" },
        { "Azeri", "AZ" },
        { "English", "EN" }
    };

    protected async Task<T> ClickSaveButton<T>() where T : BaseProjectElements
    {
        await ClickAsync(SaveButton);
        return (T)Activator.CreateInstance(typeof(T), Page)!;
    }

    private async Task<T> ClickButton<T>(ILocator buttonSelector) where T : BaseProjectElements
    {
        await WaitVisibleAsync(buttonSelector);
        await ClickAsync(buttonSelector);
        return (T)Activator.CreateInstance(typeof(T), Page)!;
    }

    private async Task<T> InputValueTo<T>(ILocator inputSelector, string text) where T : BaseProjectElements
    {
        await WaitVisibleAsync(inputSelector);
        await InputAsync(inputSelector, text);
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

        await ClickAsync(ClickCustomButton(buttonText));
        return (T)Activator.CreateInstance(typeof(T), Page)!;
    }

    protected string FormatRuntime(string runtime)
    {
        runtime = runtime.PadLeft(6, '0');

        var hours = runtime[..2];
        var minutes = runtime.Substring(2, 2);
        var seconds = runtime.Substring(4, 2);

        return $"{hours}:{minutes}:{seconds}";
    }

    public async Task<MailPage> OpenMailpitAsync()
    {
        // open Mailpit in a fresh tab
        var newPage = await OpenNewTabAsync(MailpitUrl);

        // build the Mailpit POM, giving it both pages
        var mailpit = new MailPage(newPage, Page);

        var isMailpitOpened = await WaitAndVerifySingleElementText("Mailpit", mailpit.MailpitHeaderTitle);
        isMailpitOpened.Should().BeTrue("Mailpit header title should contain its name.");

        return mailpit;
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

    protected async Task<bool> ValidateSomeDetails(string templateName, string workType, string customer,
        string? language = null)
    {
        var expectedDetails = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            { "Name", templateName },
            { "Work Type", workType },
            { "Customer", customer }
        };

        if (!string.IsNullOrEmpty(language) && _languageCodes.TryGetValue(language, out var code))
        {
            expectedDetails.Add("Language", $"{code} {language}");
        }

        return await VerifyTableDetailsAsync(Page, expectedDetails, templateName);
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

    public async Task<bool> TryNavigatePage(PageNavigationDirection direction)
    {
        var buttonLocator = direction == PageNavigationDirection.Next
            ? NextPageButton
            : PreviousPageButton;

        await WaitVisibleAsync(buttonLocator);
        await WaitForButtonToEnableAsync(buttonLocator);

        var ariaDisabled = await buttonLocator.GetAttributeAsync("aria-disabled");
        var isDisabled = ariaDisabled?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

        if (isDisabled)
        {
            Log.Warning("{PageNavigationDirection} Page button is disabled; cannot navigate.", direction);
            return false;
        }

        await ClickAsync(buttonLocator);
        Log.Information("{PageNavigationDirection} page button clicked successfully.", direction);
        return true;
    }

    protected async Task<UiActionState> GetEllipsisMenuItemStateAsync(ILocator item, string disabledClass)
    {
        if (!await WaitVisibleAsync(item))
        {
            Log.Warning("Button not found.");
            return UiActionState.Missing;
        }

        var isDisabled = await item.EvaluateAsync<bool>(
            $"e => e.classList.contains('{disabledClass}')");

        var state = isDisabled ? UiActionState.Disabled : UiActionState.Enabled;
        Log.Information("Button state: {UiActionState}", state);
        return state;
    }
    
    protected async Task<UiActionState> GetElementStateAsync(ILocator item, string elementClass)
    {
        if (!await WaitVisibleAsync(item))
        {
            Log.Warning("Element not found.");
            return UiActionState.Missing;
        }

        var isDisabled = await item.EvaluateAsync<bool>($"e => e.classList.contains('{elementClass}')");

        var state = isDisabled ? UiActionState.Disabled : UiActionState.Enabled;
        Log.Information("Element state: {UiActionState}", state);
        return state;
    }

    public static async Task<ILocator> ResolveEditableAsync(ILocator container)
    {
        var tag = (await container.EvaluateAsync<string>("el => el.tagName.toLowerCase()")).Trim();
        if (tag is "input" or "textarea") return container;

        var editable = container.Locator("input, textarea, [contenteditable='true']").First;
        await editable.ExpectSingleVisibleAsync();
        return editable;
    }
}