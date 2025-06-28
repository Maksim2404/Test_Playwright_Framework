using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.base_abstract;

namespace test.playwright.framework.utils;

public class AssertUtils(IPage page) : BasePage(page)
{
    private const string NoDataSelector = "//tr[@class='v-data-table__empty-wrapper']//td";
    private ILocator EllipsisMenuList => Page.Locator("//div[@class='v-overlay__content']//div[@role='listbox']");

    private static readonly Dictionary<string, List<string>> EllipsisMenuExpectations = new()
    {
        {
            "Project",
            [
                "View Project",
                "Edit Project",
                "Manage Languages",
                "Delete Project"
            ]
        }
    };

    private async Task<List<string>> GetEllipsisMenuOptions()
    {
        await WaitForLocatorToExistAsync(EllipsisMenuList);

        var menuItems = EllipsisMenuList.Locator("div.v-list-item-title");
        var count = await menuItems.CountAsync();
        var texts = new List<string>();

        for (var i = 0; i < count; i++)
        {
            var text = await menuItems.Nth(i).InnerTextAsync();
            texts.Add(text.Trim());
        }

        return texts;
    }

    public async Task<bool> VerifyEllipsisMenu(string entityType)
    {
        var expectedItems = EllipsisMenuExpectations[entityType];
        var actualItems = await GetEllipsisMenuOptions();

        if (actualItems.Count != expectedItems.Count)
        {
            Log.Error($"Menu count mismatch for {entityType}: " +
                      $"Expected {expectedItems.Count}, got {actualItems.Count}");
            return false;
        }

        for (var i = 0; i < expectedItems.Count; i++)
        {
            if (string.Equals(expectedItems[i], actualItems[i], StringComparison.OrdinalIgnoreCase)) continue;
            Log.Error($"Menu item mismatch at index {i}. " +
                      $"Expected '{expectedItems[i]}', got '{actualItems[i]}'.");
            return false;
        }

        Log.Information($"Ellipsis menu items for {entityType} matched perfectly.");
        return true;
    }

    protected async Task<bool> VerifyField(ILocator rowLocator, string xpath, string expectedValue,
        bool partialMatch = false)
    {
        var displayedValue = await rowLocator.Locator(xpath).TextContentAsync();
        if (displayedValue == null)
        {
            Log.Warning($"No text found for xpath: {xpath}");
            return false;
        }

        displayedValue = displayedValue.Trim();
        if (!partialMatch)
        {
            var isExact = displayedValue.Equals(expectedValue, StringComparison.Ordinal);
            Log.Information($"Verifying field exact: Expected - '{expectedValue}', Actual - '{displayedValue}'");
            return isExact;
        }
        else
        {
            var contains = displayedValue.Contains(expectedValue, StringComparison.OrdinalIgnoreCase);
            Log.Information(
                $"Verifying field partial: Expected (substring) - '{expectedValue}', Actual - '{displayedValue}'");
            return contains;
        }
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

    protected async Task<ILocator> EnsureLocatorAsync(object selectorOrLocator)
    {
        ILocator locator;
        switch (selectorOrLocator)
        {
            case string selector:
                locator = Page.Locator(selector);
                await WaitForLocatorToExistAsync(locator);
                break;
            case ILocator loc:
                await WaitForLocatorToExistAsync(loc);
                locator = loc;
                break;
            default:
                throw new ArgumentException("Invalid argument type. Must be a string or ILocator.",
                    nameof(selectorOrLocator));
        }

        return locator;
    }

    private string RemoveLeadingDigits(string rawText)
    {
        var match = Regex.Match(rawText, @"^\d+(?<someValue>.+)$");
        if (match.Success)
        {
            return match.Groups["someValue"].Value.Trim();
        }

        Log.Warning($"Task node has unexpected format: {rawText}");
        return string.Empty;
    }

    private async Task<string> GetTextFromDiv(string title, string prefix)
    {
        var selector = $"//div[b[contains(text(), '{title}')]]";
        var textContent = await Page.EvalOnSelectorAsync<string>(selector, "div => div.textContent");
        var startIndex = textContent.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length;
        var text = textContent[startIndex..].Trim();
        return text;
    }

    protected async Task<bool> IsToggledOn(ILocator toggleLocator)
    {
        await WaitForLocatorToExistAsync(toggleLocator);
        var isChecked = await toggleLocator.IsCheckedAsync();
        Log.Information($"Toggle checked state: {isChecked}");
        return isChecked;
    }

    protected async Task<bool> WaitAndVerifySingleElementText(string expectedText, object selectorOrLocator,
        bool partialMatch = false, bool ignoreCase = false)
    {
        try
        {
            var locator = await EnsureLocatorAsync(selectorOrLocator);

            var count = await locator.CountAsync();
            if (count == 0)
            {
                Log.Error($"Expected at least 1 match for locator, but found {count}.");
                return false;
            }

            var text = await locator.TextContentAsync();
            if (string.IsNullOrWhiteSpace(text))
            {
                Log.Error("Element text is null or empty.");
                return false;
            }

            text = text.Trim();
            var comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            bool matches;
            if (partialMatch)
            {
                matches = text.Contains(expectedText, StringComparison.OrdinalIgnoreCase);
                Log.Information(matches
                    ? $"Partial match found. Expected substring '{expectedText}', Actual '{text}'."
                    : $"Partial match FAILED. Expected substring '{expectedText}', Actual '{text}'.");
            }
            else
            {
                matches = text.Equals(expectedText, comparisonType);
                Log.Information(matches
                    ? $"Exact match succeeded. Expected '{expectedText}', Actual '{text}'."
                    : $"Exact match FAILED. Expected '{expectedText}', Actual '{text}'.");
            }

            return matches;
        }
        catch (TimeoutException tex)
        {
            Log.Error($"Timeout waiting for the element to be visible. {tex.Message}");
            return false;
        }
        catch (PlaywrightException ex)
        {
            Log.Error($"Error while waiting/verifying single element text: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> VerifyNoDataFound(string text)
    {
        var noDataStatusDisplayed = await WaitAndVerifySingleElementText(text, NoDataSelector);

        if (!noDataStatusDisplayed)
        {
            Log.Warning("Searched data present on the page.");
            return false;
        }

        Log.Information("Searched data isn't present on the page and search functionality works as expected");
        return true;
    }

    private static string Pretty(object value) => value switch
    {
        IEnumerable<string> list when value is not string =>
            string.Join(", ", list),

        null => "«null»",
        _ => value.ToString()!
    };

    private static string Normalise(string s) => Regex.Replace(s, @"\s+", " ").Trim();

    protected async Task<bool> VerifyTableDetailsAsync(Dictionary<string, object> expectedDetails, string tableName)
    {
        var mismatches = new List<string>();
        foreach (var (key, expectedValue) in expectedDetails)
        {
            var cell = Page.Locator($"//tr[td[1][normalize-space(text())='{key}']]/td[2]");

            if (!await WaitForLocatorToExistAsync(cell))
            {
                mismatches.Add($"Row “{key}” not found");
                continue;
            }

            var cellText = (await cell.InnerTextAsync())?.Trim() ?? string.Empty;

            var match = expectedValue switch
            {
                string s => Normalise(cellText).Equals(Normalise(s), StringComparison.OrdinalIgnoreCase),

                IEnumerable<string> list when expectedValue is not string =>
                    list.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(
                        (await cell.Locator("//div[contains(@class,'v-chip__content')]").AllInnerTextsAsync())
                        .Select(t => t.Trim())
                        .ToHashSet(StringComparer.OrdinalIgnoreCase)),

                _ => throw new InvalidOperationException(
                    $"Unsupported expected‑value type ({expectedValue.GetType().Name}) for “{key}”.")
            };

            if (!match)
                mismatches.Add($"Row “{key}”: expected “{Pretty(expectedValue)}”, got “{cellText}”.");
        }

        if (mismatches.Count == 0)
        {
            Log.Information($"All properties for “{tableName}” verified successfully.");
            return true;
        }

        mismatches.ForEach(Log.Warning);
        return false;
    }
}