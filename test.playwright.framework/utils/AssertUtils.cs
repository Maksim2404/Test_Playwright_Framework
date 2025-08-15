using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.base_abstract;
using test.playwright.framework.pages.enums;

namespace test.playwright.framework.utils;

public partial class AssertUtils(IPage page) : BasePage(page)
{
    private ILocator EllipsisMenuList => Page.Locator("//div[@class='v-overlay__content']//div[@role='listbox']");
    private ILocator TaskLabelLocator => Page.Locator("//div[@class='task-node-content']/p");

    private static readonly IReadOnlyDictionary<EllipsisEntity, IReadOnlyList<string>> EllipsisMenuExpectations =
        new Dictionary<EllipsisEntity, IReadOnlyList<string>>
        {
            [EllipsisEntity.Project] =
            [
                "View Project",
                "Edit Project",
                "Manage Languages",
                "Delete Project"
            ],
            [EllipsisEntity.Template] =
            [
                "Edit Template",
                "Manage Template",
                "Clone Template",
                "Delete Template"
            ],
            [EllipsisEntity.User] =
            [
                "Edit User",
                "View Assigned Tasks",
                "Manage Notifications",
                "Delete Mapping"
            ]
        };

    [GeneratedRegex(@"^\d+(?<taskType>.+)$", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex MyRegex();

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

    public async Task<bool> VerifyEllipsisMenu(EllipsisEntity entityType)
    {
        var expectedItems = EllipsisMenuExpectations[entityType];
        var actualItems = await GetEllipsisMenuOptions();

        if (actualItems.Count != expectedItems.Count)
        {
            Log.Error("Menu count mismatch for {Type}: expected {E}, got {A}", entityType, expectedItems.Count,
                actualItems.Count);
            return false;
        }

        for (var i = 0; i < expectedItems.Count; i++)
        {
            if (!string.Equals(expectedItems[i], actualItems[i], StringComparison.OrdinalIgnoreCase))
            {
                Log.Error("Menu item mismatch at {Idx}: exp='{Exp}' got='{Got}'", i, expectedItems[i], actualItems[i]);
                return false;
            }
        }

        Log.Information("Ellipsis menu items matched for {Type}.", entityType);
        return true;
    }

    protected delegate bool FieldComparer(string actual, object expected);

    protected static readonly FieldComparer Exact = (a, e) =>
        string.Equals(a, e.ToString(), StringComparison.OrdinalIgnoreCase);

    protected static readonly FieldComparer Contains = (a, e) =>
        a.Contains(e.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    protected async Task<bool> VerifyFieldAsync(ILocator cell, object? expected, FieldComparer? comparer = null)
    {
        var count = await cell.CountAsync();

        bool ok;
        if (count == 0)
        {
            ok = expected is null;
            Log.Debug("Verify  <missing element>  vs  «{Expected}»  ⇒  {Ok}", expected, ok);
            return ok;
        }

        var txt = (await cell.InnerTextAsync())?.Trim() ?? "";

        if (expected is null)
        {
            ok = string.IsNullOrWhiteSpace(txt);
            Log.Debug("Verify  «{Actual}»  is empty  ⇒  {Ok}", txt, ok);
        }
        else
        {
            ok = (comparer ?? Exact)(txt, expected);
            Log.Debug("Verify  «{Actual}»  vs  «{Expected}»  ⇒  {Ok}", txt, expected, ok);
        }

        return ok;
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

    private string StripTaskTypeLeadingDigits(string rawText)
    {
        var match = MyRegex().Match(rawText);
        if (match.Success)
        {
            return match.Groups["taskType"].Value.Trim();
        }

        Log.Warning($"Task node has unexpected format: {rawText}");
        return string.Empty;
    }

    private async Task<IReadOnlyList<string>> GetDiagramTaskDetailsWithIdsAsync()
    {
        await TaskLabelLocator.ExpectVisibleCountAsync();
        var rawLabels = await TaskLabelLocator.AllInnerTextsAsync();

        var cleaned = rawLabels.Select((t, i) =>
        {
            var trimmed = t.Trim();
            Log.Information("Node {Idx}: «{Text}»", i, trimmed);
            return trimmed;
        }).ToArray();

        Log.Information("Found {Count} nodes in the diagram.", cleaned.Length);
        return cleaned;
    }

    public async Task<bool> VerifyDiagramTaskDependencies(params TaskTypeKind[] expectedTaskTypes)
    {
        var actualTasks = await GetDiagramTaskDetailsWithIdsAsync();
        var actual = actualTasks
            .Select(StripTaskTypeLeadingDigits)
            .Where(t => t.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = expectedTaskTypes
            .Where(e => !actual.Contains(e.ToUi()))
            .ToArray();

        if (missing.Length == 0)
        {
            Log.Information("All expected task types are present in the diagram.");
            return true;
        }

        Log.Warning("Missing task types: {Missing}", string.Join(", ", missing));
        return false;
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
            await locator.ExpectVisibleCountAsync();

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

    private static string Pretty(object value) => value switch
    {
        IEnumerable<string> list when value is not string =>
            string.Join(", ", list),

        null => "«null»",
        _ => value.ToString()!
    };

    private static string Normalise(string s) => Regex.Replace(s, @"\s+", " ").Trim();

    protected async Task<bool> VerifyTableDetailsAsync(Func<string, ILocator> locatorProvider,
        Dictionary<string, object> expectedDetails, string tableName)
    {
        var mismatches = new List<string>();
        foreach (var (key, expectedValue) in expectedDetails)
        {
            var cell = locatorProvider($"//tr[td[1][normalize-space(text())='{key}']]/td[2]");

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
                        (await cell.Locator("//span[contains(@class,'v-chip__content')]").AllInnerTextsAsync())
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
            Log.Information($"All properties for asset “{tableName}” verified successfully.");
            return true;
        }

        mismatches.ForEach(Log.Warning);
        return false;
    }

    protected Task<bool> VerifyTableDetailsAsync(IPage root, Dictionary<string, object> expected, string tableName)
        => VerifyTableDetailsAsync(s => root.Locator(s), expected, tableName);

    protected Task<bool> VerifyTableDetailsAsync(IFrameLocator root, Dictionary<string, object> expected,
        string tableName) => VerifyTableDetailsAsync(s => root.Locator(s), expected, tableName);
}