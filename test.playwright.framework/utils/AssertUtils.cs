using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.base_abstract;

namespace test.playwright.framework.utils;

public class AssertUtils(IPage page) : BasePage(page)
{
    private const string NoDataSelector = "//tr[@class='v-data-table__empty-wrapper']//td";

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