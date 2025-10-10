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
    private ILocator HighlightedNoteText(string className) => Page.Locator($"//mark[@class='{className}']");

    private static readonly IReadOnlyDictionary<EllipsisEntity, IReadOnlyList<string>> EllipsisEntityExt =
        new Dictionary<EllipsisEntity, IReadOnlyList<string>>
        {
            [EllipsisEntity.Project] =
            [
                "View Project",
                "Edit Project",
                "Manage Languages",
                "Delete Project"
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
    
    public async Task<bool> IsHighlightedNotesColorCorrect(HighlightMarkerKind highlightColor)
    {
        Log.Information("Attempting to verify highlight color: {ToUi}.", highlightColor.ToUi());

        if (!HighlightMarkerExt.ColorToClassMap.TryGetValue(highlightColor, out var className))
        {
            Log.Information("Color description {HighlightMarkerKind} not found in mapping.", highlightColor);
            return false;
        }

        await ReadyAsync(HighlightedNoteText(className));

        var elements = await HighlightedNoteText(className).ElementHandlesAsync();
        var isHighlighted = elements.Count > 0;

        Log.Information(isHighlighted
            ? $"Successfully found highlighted text with color: {highlightColor}."
            : $"No highlighted text found with color: {highlightColor}.");

        return isHighlighted;
    }

    private async Task<bool> IsErrorPopupPresent(ILocator popup)
    {
        var errorPresent = await WaitVisibleAsync(popup);

        Log.Information(errorPresent
            ? $"Popup {popup} appeared."
            : $"Popup {popup} did not appear.");

        return errorPresent;
    }

    private async Task<IReadOnlyList<string>> GetEllipsisMenuOptions()
    {
        await WaitVisibleAsync(EllipsisMenuList);
        var menuItems = EllipsisMenuList.Locator("div.v-list-item-title");
        return (await menuItems.AllInnerTextsAsync()).Select(s => s.Trim()).ToArray();
    }

    public async Task<bool> VerifyEllipsisMenu(EllipsisEntity entityType)
    {
        var expectedItems = EllipsisEntityExt[entityType];
        var actualItems = await GetEllipsisMenuOptions();

        var ok = expectedItems.SequenceEqual(actualItems, StringComparer.OrdinalIgnoreCase);
        if (!ok)
        {
            Log.Error("Ellipsis mismatch. Expected: [{E}] Got: [{A}]", string.Join(", ", expectedItems),
                string.Join(", ", actualItems));
        }

        return ok;
    }

    protected delegate bool FieldComparer(string actual, object expected);

    protected static readonly FieldComparer Exact = (a, e) =>
        string.Equals(a, e.ToString(), StringComparison.OrdinalIgnoreCase);

    protected static readonly FieldComparer Contains = (a, e) =>
        a.Contains(e.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    protected async Task<bool> VerifyFieldAsync(ILocator cell, object? expected, FieldComparer? comparer = null)
    {
        await cell.ExpectSingleVisibleAsync();
        var count = await cell.CountAsync();

        bool ok;
        if (count == 0)
        {
            ok = expected is null;
            Log.Information("Verify  <missing element>  vs  «{Expected}»  ⇒  {Ok}", expected, ok);
            return ok;
        }

        var txt = (await cell.InnerTextAsync()).Trim();

        if (expected is null)
        {
            ok = string.IsNullOrWhiteSpace(txt);
            Log.Information("Verify  «{Actual}»  is empty  ⇒  {Ok}", txt, ok);
        }
        else
        {
            ok = (comparer ?? Exact)(txt, expected);
            Log.Information("Verify  «{Actual}»  vs  «{Expected}»  ⇒  {Ok}", txt, expected, ok);
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
                await WaitVisibleAsync(locator);
                break;
            case ILocator loc:
                await WaitVisibleAsync(loc);
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
    
    private static async Task<bool> TextEqualsAsync(ILocator loc, string expected, bool ignoreCase = false,
        bool collapseWhitespace = true)
    {
        var actual = await loc.InnerTextAsync();
        return TextMatch.Equals(actual, expected, ignoreCase, collapseWhitespace);
    }

    private static async Task<bool> TextContainsAsync(ILocator loc, string expected, bool ignoreCase = false,
        bool collapseWhitespace = true)
    {
        var actual = await loc.InnerTextAsync();
        return TextMatch.Contains(actual, expected, ignoreCase, collapseWhitespace);
    }

    protected async Task<bool> WaitAndVerifySingleElementText(string expectedText, ILocator selectorOrLocator,
        bool partialMatch = false, bool ignoreCase = false, bool collapseWhitespace = true)
    {
        try
        {
            var locator = await EnsureLocatorAsync(selectorOrLocator);
            await locator.ExpectVisibleCountAsync();

            var matches = partialMatch
                ? await TextContainsAsync(locator, expectedText, ignoreCase, collapseWhitespace)
                : await TextEqualsAsync(locator, expectedText, ignoreCase, collapseWhitespace);

            var actual = TextMatch.Normalize(await locator.InnerTextAsync(), collapseWhitespace);

            if (matches)
            {
                Log.Information("Text match succeeded. Mode={Mode}, Expected='{Exp}', Actual='{Act}'",
                    partialMatch ? "contains" : "equals", expectedText, actual);
            }
            else
            {
                Log.Warning("Text match FAILED. Mode={Mode}, Expected='{Exp}', Actual='{Act}'",
                    partialMatch ? "contains" : "equals", expectedText, actual);
            }

            return matches;
        }
        catch (PlaywrightException ex)
        {
            Log.Error("Playwright error while verifying element text. {Msg}", ex.Message);
            return false;
        }
    }
    
    private async Task<bool> IsToggledOn(ILocator toggleLocator)
    {
        var isChecked = await toggleLocator.IsCheckedAsync();
        Log.Information("Toggle checked state: {IsChecked}", isChecked);
        return isChecked;
    }

    protected async Task EnsureToggleStateAsync(ILocator toggle, bool shouldBeOn)
    {
        var isOn = await IsToggledOn(toggle);
        if (isOn != shouldBeOn)
        {
            await ClickAsync(toggle);
            if (shouldBeOn) await Assertions.Expect(toggle).ToBeCheckedAsync();
            else await Assertions.Expect(toggle).Not.ToBeCheckedAsync();
        }
    }

    private static string Pretty(object value) => value switch
    {
        IEnumerable<string> list when value is not string => string.Join(", ", list),

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

            if (!await WaitVisibleAsync(cell))
            {
                mismatches.Add($"Row “{key}” not found");
                continue;
            }

            var cellText = (await cell.InnerTextAsync()).Trim();

            var match = expectedValue switch
            {
                string s => TextMatch.Normalize(cellText)
                    .Equals(TextMatch.Normalize(s), StringComparison.OrdinalIgnoreCase),

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
            Log.Information("All properties for object “{TableName}” verified successfully.", tableName);
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