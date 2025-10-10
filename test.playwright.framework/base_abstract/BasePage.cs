using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.utils;

namespace test.playwright.framework.base_abstract;

public record UiTimeouts(int ShortMs = 3000, int DefaultMs = 15000, int LongMs = 20000);

public abstract class BasePage(IPage page, UiTimeouts? timeouts = null)
{
    protected UiTimeouts T { get; } = timeouts ?? new UiTimeouts();
    
    protected internal IPage Page { get; } =
        page ?? throw new ArgumentNullException(nameof(page), "Page cannot be null");

    protected async Task<IPage> OpenNewTabAsync(string url, WaitUntilState wait = WaitUntilState.Load)
    {
        var newPage = await Page.Context.NewPageAsync();
        await newPage.GotoAsync(url, new PageGotoOptions { WaitUntil = wait });
        return newPage;
    }

    //that will just wait for the opened page to load but the page itself opens in a new tab by design!!!
    protected async Task<IPage> OpenNewPageAsync(Func<Task> actionToOpenPage)
    {
        var newPageTask = Page.Context.WaitForPageAsync();

        await actionToOpenPage();

        var newPage = await newPageTask;
        await newPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return newPage;
    }

    protected internal async Task<IPage?> CloseCurrentPageAndSwitchBackAsync()
    {
        var pages = Page.Context.Pages;
        IPage? target = null;

        for (var i = 0; i < pages.Count; i++)
        {
            if (!ReferenceEquals(pages[i], Page)) continue;
            target = i > 0 ? pages[i - 1] : pages.Count > 1 ? pages[1] : null;
            break;
        }

        await Page.CloseAsync();
        if (target != null) await target.BringToFrontAsync();
        return target;
    }

    protected async Task WaitForNetworkIdle()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
    }

    private async Task WaitForDomContentLoaded()
    {
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded,
            new PageWaitForLoadStateOptions { Timeout = 15000 });
    }

    protected async Task<bool> ReadyAsync(ILocator locator, int timeoutMs = 0)
    {
        var t = timeoutMs <= 0 ? T.DefaultMs : timeoutMs;
        try
        {
            await Assertions.Expect(locator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = t });
            await Assertions.Expect(locator).ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions { Timeout = t });
            Log.Information("Element '{Locator}' is visible and enabled. Ready for interaction.", locator);
            return true;
        }

        catch (PlaywrightException ex)
        {
            Log.Error("Element '{Locator}' not ready within {TimeoutMs}ms: {Msg}", locator, t, ex.Message);
            return false;
        }
    }
    
    public async Task<bool> WaitVisibleAsync(ILocator locator, int timeoutMs = 15000)
    {
        timeoutMs = timeoutMs <= 0 ? T.DefaultMs : timeoutMs;
        try
        {
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeoutMs
            });

            Log.Information("Locator {Locator} became visible within {TimeoutMs}ms.", locator, timeoutMs);
            return true;
        }
        catch (PlaywrightException ex)
        {
            Log.Warning("Timeout waiting for locator to appear - {ExMessage}", ex.Message);
            return false;
        }
    }
    
    protected async Task<bool> WaitAbsentAsync(ILocator locator, int timeoutMs = 0)
    {
        timeoutMs = timeoutMs <= 0 ? T.DefaultMs : timeoutMs;
        try
        {
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Detached,
                Timeout = timeoutMs
            });

            return true;
        }
        catch (TimeoutException)
        {
            try
            {
                await locator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Hidden,
                    Timeout = timeoutMs
                });

                return true;
            }
            catch
            {
                Log.Error("Locator still present after timeout.");
                return false;
            }
        }
    }
    
    public Task<string> GetTitleAsync()
    {
        var title = Page.TitleAsync();
        Log.Information("Page title retrieved: {Title}", title);
        return title;
    }

    protected async Task HoverAsync(ILocator locator, int timeoutMs = 0)
    {
        if (!await ReadyAsync(locator, timeoutMs))
            throw new PlaywrightException($"Locator not ready for click: {locator}");
        await locator.HoverAsync(new LocatorHoverOptions { Timeout = timeoutMs > 0 ? timeoutMs : T.DefaultMs });
        Log.Debug("Hovered over element: {Locator}", locator);
    }

    protected async Task ClickAsync(ILocator locator)
    {
        await HoverAsync(locator);
        await locator.ClickAsync();
        Log.Debug("Clicked on element: {Locator}", locator);
    }

    protected async Task MouseClickCenterAsync(ILocator locator)
    {
        if (!await ReadyAsync(locator)) throw new PlaywrightException($"Locator not ready: {locator}");
        var box = await locator.BoundingBoxAsync();
        if (box is null) throw new PlaywrightException("Bounding box not available");
        await Page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
    }

    protected async Task DoubleClickAsync(ILocator locator, int timeoutMs = 0)
    {
        if (!await ReadyAsync(locator, timeoutMs))
            throw new PlaywrightException($"Locator not ready for double‑click: {locator}");

        await locator.DblClickAsync(new LocatorDblClickOptions { Timeout = timeoutMs > 0 ? timeoutMs : T.DefaultMs });
    }

    protected async Task ClickByJavaScript(string selector)
    {
        await Page.EvaluateAsync(@"(selector) => {const element = document.querySelector(selector);
        if (element) element.click(); else throw new Error('Element not found');}", selector);
    }

    public int GetListSize(List<IElementHandle> list)
    {
        return list.Count;
    }

    public async Task DragAndDrop(ILocator sourceSelector, ILocator targetSelector)
    {
        try
        {
            var isSourceVisibleAndEnabled = await ReadyAsync(sourceSelector);
            var isTargetVisibleAndEnabled = await ReadyAsync(targetSelector);

            if (isSourceVisibleAndEnabled && isTargetVisibleAndEnabled)
            {
                {
                    var sourceElement = sourceSelector.First;
                    {
                        var targetElement = targetSelector.First;

                        await sourceElement.DragToAsync(targetElement);
                    }
                }
            }
            else
            {
                throw new Exception("Drag and drop action failed due to element visibility or enablement issue.");
            }
        }
        catch (PlaywrightException ex)
        {
            throw new Exception("Drag and drop action failed.", ex);
        }
    }

    protected async Task ClearAsync(ILocator locator)
    {
        await locator.FillAsync(string.Empty);
        Log.Information("Cleared content of element: {Locator}", locator);
    }

    protected async Task HardClearAsync(ILocator input)
    {
        await ClickAsync(input);
        await input.PressAsync("Control+A");
        await input.PressAsync("Delete");
    }

    private static string DigitsOnly(string s) => Regex.Replace(s, @"\D", "");

    protected async Task InputAsync(ILocator locator, string text)
    {
        await locator.ClickAsync();
        await locator.ClearAsync();
        await locator.FillAsync(text);
        var tag = await locator.EvaluateAsync<string>("e => e.tagName.toLowerCase()");
        if (tag is "input" or "textarea")
        {
            var actual = await locator.InputValueAsync();
            if (DigitsOnly(actual) != DigitsOnly(text))
                await Assertions.Expect(locator).ToHaveValueAsync(text);
        }
        else
        {
            var actual = await locator.InnerTextAsync();
            if (DigitsOnly(actual) != DigitsOnly(text))
                await Assertions.Expect(locator).ToHaveTextAsync(text);
        }

        Log.Information("Filled text '{Text}' into element: {Locator}", text, locator);
    }

    protected async Task InputNumberAsync(ILocator locator, int value)
    {
        var expected = value.ToString(CultureInfo.InvariantCulture);

        await ClickAsync(locator);
        await ClearAsync(locator);
        await locator.FillAsync(expected);

        var regex = new Regex($"^0*{Regex.Escape(expected)}$");
        await Assertions.Expect(locator).ToHaveValueAsync(regex);
    }

    protected async Task TypeAsync(ILocator locator, string text, int delayMs = 50)
    {
        await ClickAsync(locator);
        if (delayMs > 0) await Page.Keyboard.TypeAsync(text, new KeyboardTypeOptions { Delay = delayMs });
        else await locator.FillAsync(text);
        Log.Information("Inputted text by keyboard '{Text}' into element: {Locator}", text, locator);
    }

    protected static async Task<string> GetTextAsync(ILocator locator)
    {
        try
        {
            await locator.ExpectSingleVisibleAsync();

            var text = (await locator.InnerTextAsync()).Trim();
            Log.Information("Successfully retrieved text from XPath '{Locator}': '{Text}'", locator, text);
            return text;
        }
        catch (PlaywrightException ex)
        {
            Log.Error("An error occurred while trying to get text from '{Locator}': {ExMessage}", locator, ex.Message);
            return string.Empty;
        }
    }

    protected async Task<bool> EnsureListboxClosedAsync()
    {
        var listbox = Page.GetByRole(AriaRole.Listbox);
        if (await listbox.CountAsync() == 0) return true;

        await PressEscapeAsync();
        return await WaitAbsentAsync(listbox, T.ShortMs);
    }

    protected async Task<ILocator> EnsureLocatorAsync(ILocator locator)
    {
        await WaitVisibleAsync(locator);
        return locator;
    }

    public async Task<string> GetTextByXPathAsync(ILocator locator)
    {
        try
        {
            await ReadyAsync(locator);
            var element = await locator.ElementHandleAsync();

            var text = await element.InnerTextAsync();
            Log.Information("Successfully retrieved text from XPath '{Locator}': '{Text}'", locator, text);
            return text;
        }
        catch (PlaywrightException ex)
        {
            Log.Error("An error occurred while trying to get text from '{Locator}': {ExMessage}", locator, ex.Message);
            return string.Empty;
        }
    }

    public async Task PressEscapeAsync()
    {
        await Page.Keyboard.PressAsync("Escape");
        Log.Information("Clicked 'Escape' button by keyword");
    }

    protected internal async Task RefreshPage()
    {
        await Page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.Load });
        Log.Information("Page Refreshed");
    }

    protected async Task PressEnterAsync()
    {
        await Page.Keyboard.PressAsync("Enter");
        Log.Information("Clicked 'Enter' button by keyword");
    }

    public async Task GoBack()
    {
        try
        {
            await Page.GoBackAsync(new PageGoBackOptions { WaitUntil = WaitUntilState.NetworkIdle });
            Log.Information("Navigated back and page is ready");
        }
        catch (PlaywrightException ex)
        {
            Log.Error("Failed to go back: {ExMessage}", ex.Message);
            throw;
        }
    }

    protected async Task<bool> ClickSaveShortcutByKeyboard(ILocator? successSelector = null)
    {
        try
        {
            await Page.Keyboard.PressAsync("Control+s");
            Log.Information("Pressed 'Save' shortcut (Ctrl+S) on the keyboard.");

            await WaitForNetworkIdle();

            if (successSelector != null) await WaitVisibleAsync(successSelector);
            Log.Information("Save shortcut action completed successfully.");
            return true;
        }
        catch (TimeoutException ex)
        {
            Log.Error($"Save shortcut action failed to confirm success within timeout: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error($"Unexpected error during 'Save' shortcut action: {ex.Message}");
            return false;
        }
    }
}