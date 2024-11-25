using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.constants;

namespace test.playwright.framework.base_abstract;

public class BaseProjectElements : BasePage
{
    private static readonly Random Random = new();
    private const string NoDataSelector = "//tr[@class='v-data-table__empty-wrapper']//td";

    protected BaseProjectElements(IPage page) : base(page)
    {
    }

    private async Task<T> ClickButton<T>(string buttonSelector) where T : BaseProjectElements
    {
        await WaitForSelectorToExistAsync(buttonSelector);
        await Click(buttonSelector);
        return (T)Activator.CreateInstance(typeof(T), Page)!;
    }

    private async Task<T> InputValueTo<T>(string inputSelector, string text) where T : BaseProjectElements
    {
        await WaitForSelectorToExistAsync(inputSelector);
        await Input(inputSelector, text);
        return (T)Activator.CreateInstance(typeof(T), Page)!;
    }

    public async Task RefreshPage()
    {
        await Page.ReloadAsync();
        await WaitForNetworkIdle();
    }

    public async Task ClickEscapeByKeyboard()
    {
        await Page.Keyboard.PressAsync("Escape");
        Log.Information("Clicked 'Escape' button by keyword");
        await WaitForNetworkIdle();
    }

    protected async Task ClickEnterByKeyboard()
    {
        await Page.Keyboard.PressAsync("Enter");
        Log.Information("Clicked 'Enter' button by keyword");
        await WaitForNetworkIdle();
    }

    public static string GenerateRandomYear()
    {
        var currentYear = DateTime.Now.Year;
        var randomYear = Random.Next(2023, currentYear + 1);

        return randomYear.ToString();
    }

    public async Task ClickGoToPreviousPage()
    {
        await Page.GoBackAsync();
        await WaitForNetworkIdle();
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

    protected static void Shuffle<T>(IList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = Random.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    protected async Task<bool> VerifyField(ILocator rowLocator, string xpath, string expectedValue)
    {
        var displayedValue = await rowLocator.Locator(xpath).TextContentAsync();
        var match = displayedValue != null && displayedValue.Trim().Equals(expectedValue);
        Log.Information($"Verifying field: Expected - {expectedValue}, Displayed - {displayedValue}");
        return match;
    }

    protected async Task<bool> VerifyFieldContainsText(ILocator rowLocator, string xpath, string expectedValue)
    {
        var displayedValue = await rowLocator.Locator(xpath).TextContentAsync();

        if (displayedValue == null)
        {
            Log.Warning($"Displayed value is null for xpath: {xpath}");
            return false;
        }

        var match = displayedValue.Trim().Contains(expectedValue, StringComparison.OrdinalIgnoreCase);
        Log.Information(
            $"Verifying field: Expected (substring) - {expectedValue}, Displayed - {displayedValue.Trim()}");
        return match;
    }

    private async Task<ILocator> ResolveLocatorAsync(object selectorOrLocator)
    {
        ILocator locator;
        switch (selectorOrLocator)
        {
            case string selector:
                await WaitForSelectorToExistAsync(selector);
                locator = Page.Locator(selector);
                break;
            case ILocator loc:
                await WaitForLocator(loc);
                locator = loc;
                break;
            default:
                throw new ArgumentException("Invalid argument type. Must be a string or ILocator.",
                    nameof(selectorOrLocator));
        }

        return locator;
    }

    protected async Task<bool> VerifyText(string text, object selectorOrLocator)
    {
        var locator = await ResolveLocatorAsync(selectorOrLocator);

        if (await locator.CountAsync() == 0)
        {
            Log.Warning($"No text found within this '{text}' value.");
            return false;
        }

        var displayedValue = await locator.TextContentAsync();
        var textMatch = displayedValue != null && displayedValue.Trim().Equals(text);
        Log.Information($"Verifying field: Expected - {text}, Displayed - {displayedValue}");
        return textMatch;
    }

    private static async Task WaitForLocator(ILocator locator)
    {
        try
        {
            await locator.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        }
        catch (TimeoutException ex)
        {
            Log.Error($"Timeout waiting for selector: {locator} - {ex.Message}");
            throw;
        }
    }

    protected async Task<bool> VerifyPopupText(string expectedText, object selectorOrLocator)
    {
        try
        {
            var locator = await ResolveLocatorAsync(selectorOrLocator);

            if (await locator.CountAsync() == 0)
            {
                Log.Warning("Popup did not appear.");
                return false;
            }

            var text = await locator.TextContentAsync();
            if (text != null && text.Trim().Equals(expectedText))
            {
                Log.Information($"Popup verification successful: '{expectedText}' was found.");
                return true;
            }
            else
            {
                Log.Warning($"Text did not match. Expected: '{expectedText}', Found: '{text}'.");
                return false;
            }
        }
        catch (TimeoutException)
        {
            Log.Warning("Failed to find the popup within the specified timeout.");
            return false;
        }
    }

    public async Task<bool> VerifyNoDataFound(string text)
    {
        var noDataStatusDisplayed = await VerifyText(text, NoDataSelector);

        if (!noDataStatusDisplayed)
        {
            Log.Warning("Searched data present on the page.");
            return false;
        }

        Log.Information("Searched data isn't present on the page and search functionality works as expected");
        return true;
    }
}