using System.Diagnostics;
using Microsoft.Playwright;
using Serilog;

namespace test.playwright.framework.base_abstract;

public abstract class BasePage(IPage page)
{
    protected IPage Page { get; } = page ?? throw new ArgumentNullException(nameof(page), "Page cannot be null");

    private ILocator GetLocator(string selector)
    {
        return Page.Locator(selector.StartsWith("xpath=") ? selector[6..] : selector);
    }

    private async Task WaitForNetworkIdle()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
    }

    private async Task WaitForDomContentLoaded()
    {
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded,
            new PageWaitForLoadStateOptions { Timeout = 15000 });
    }

    protected async Task<bool> VerifyElementVisibleAndEnable(string selector)
    {
        try
        {
            await Page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = 15000 });
            await Page.WaitForSelectorAsync(selector,
                new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });

            var isVisible = await GetLocator(selector).IsVisibleAsync();
            var isEnabled = await GetLocator(selector).IsEnabledAsync();
            return isVisible && isEnabled;
        }
        catch (PlaywrightException ex)
        {
            Log.Error($"Failed to verify if element with selector '{selector}' is visible and enabled: {ex.Message}");
        }

        return false;
    }

    protected async Task HoverToElement(string selector)
    {
        if (selector == null)
        {
            throw new ArgumentNullException(nameof(selector), "Selector cannot be null.");
        }

        try
        {
            await WaitForNetworkIdle();
            if (await VerifyElementVisibleAndEnable(selector))
            {
                await GetLocator(selector).HoverAsync();

                await WaitForNetworkIdle();
                Log.Information($"Hovered over element: {selector}");
            }
        }
        catch (PlaywrightException ex)
        {
            Log.Error($"Failed to hover over element with selector '{selector}': {ex.Message}");
            throw;
        }
    }

    protected async Task Click(string selector)
    {
        try
        {
            await WaitForNetworkIdle();
            if (await VerifyElementVisibleAndEnable(selector))
            {
                await GetLocator(selector).ClickAsync();

                await WaitForNetworkIdle();
                Log.Information($"Clicked on element: {selector}");
            }
        }
        catch (PlaywrightException ex)
        {
            Log.Error($"Failed to click on element with selector '{selector}': {ex.Message}");
            throw;
        }
    }

    protected async Task ClickByMouse(string selector)
    {
        await VerifyElementVisibleAndEnable(selector);
        var element = GetLocator(selector);
        var boundingBox = await element.BoundingBoxAsync();
        {
            if (boundingBox != null)
            {
                var x = boundingBox.X + boundingBox.Width / 2;
                var y = boundingBox.Y + boundingBox.Height / 2;
                await Page.Mouse.ClickAsync(x, y);
                Log.Information($"Clicked on element {selector} by mouse!");
            }
            else
            {
                Log.Information("Element's bounding box is not available");
            }
        }
    }

    protected async Task DoubleClick(string selector)
    {
        try
        {
            await WaitForNetworkIdle();
            await VerifyElementVisibleAndEnable(selector);
            await GetLocator(selector).DblClickAsync();

            await WaitForNetworkIdle();
            Log.Information($"Double-clicked on element: {selector}");
        }
        catch (PlaywrightException ex)
        {
            Log.Error($"Failed to double-click on element with selector '{selector}': {ex.Message}");
            throw;
        }
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

    public async Task<string> GetTitle()
    {
        var title = await Page.TitleAsync();
        Log.Information($"Page title retrieved: {title}");
        return title;
    }

    public async Task DragAndDrop(string sourceSelector, string targetSelector)
    {
        try
        {
            var isSourceVisibleAndEnabled = await VerifyElementVisibleAndEnable(sourceSelector);
            var isTargetVisibleAndEnabled = await VerifyElementVisibleAndEnable(targetSelector);

            if (isSourceVisibleAndEnabled && isTargetVisibleAndEnabled)
            {
                {
                    var sourceElement = GetLocator(sourceSelector).First;
                    {
                        var targetElement = GetLocator(targetSelector).First;

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

    protected async Task Clear(string selector)
    {
        await VerifyElementVisibleAndEnable(selector);
        await GetLocator(selector).FillAsync(string.Empty);
        await WaitForNetworkIdle();
        Log.Information($"Cleared content of element: {selector}");
    }

    protected async Task Input(string selector, string text)
    {
        try
        {
            await WaitForNetworkIdle();
            if (await VerifyElementVisibleAndEnable(selector))
            {
                await GetLocator(selector).ClickAsync();
                await GetLocator(selector).ClearAsync();
                await GetLocator(selector).FillAsync(text);

                await GetLocator(selector).WaitForAsync(new LocatorWaitForOptions
                    { State = WaitForSelectorState.Visible, Timeout = 15000 });

                await WaitForNetworkIdle();
                Log.Information($"Filled text '{text}' into element: {selector}");
            }
        }
        catch (PlaywrightException ex)
        {
            Log.Error($"Failed to fill text '{text}' into element with selector '{selector}': {ex.Message}");
            throw;
        }
    }

    protected async Task InputByKeyboard(string selector, string text)
    {
        await VerifyElementVisibleAndEnable(selector);
        await Click(selector);
        await Page.Keyboard.TypeAsync(text, new KeyboardTypeOptions { Delay = 50 });
        await WaitForNetworkIdle();
        Log.Information($"Inputted text by keyboard '{text}' into element: {selector}");
    }

    public async Task ScrollToElement(string selector)
    {
        var elementHandle = await Page.QuerySelectorAsync(selector);
        if (elementHandle != null)
        {
            var boundingBox = await elementHandle.BoundingBoxAsync();

            if (boundingBox != null)
            {
                var x = boundingBox.X + boundingBox.Width / 2;
                var y = boundingBox.Y + boundingBox.Height / 2;

                await Page.Mouse.MoveAsync(x, y);
            }
        }
    }

    protected async Task GoBack()
    {
        try
        {
            await Page.GoBackAsync();
            await WaitForNetworkIdle();
            Log.Information("Navigated back and page is ready");
        }
        catch (PlaywrightException ex)
        {
            Log.Error($"Failed to go back: {ex.Message}");
            throw;
        }
    }

    protected async Task<bool> WaitForSelectorToExistAsync(string selector, bool expectToExist = true)
    {
        await WaitForNetworkIdle();
        await WaitForDomContentLoaded();

        try
        {
            var elementHandle = await Page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
            {
                /*State = expectToExist ? WaitForSelectorState.Attached : WaitForSelectorState.Detached,*/
                Timeout = expectToExist ? 20000 : 5000
            });

            if (elementHandle != null && expectToExist)
            {
                Log.Information($"Element with selector '{selector}' is visible on the page.");
                return true;
            }
            else if (elementHandle == null && !expectToExist)
            {
                Log.Information($"Confirmed absence of element with selector '{selector}'.");
                return true;
            }
        }
        catch (TimeoutException ex)
        {
            Log.Information(
                $"Element with selector '{selector}' not found or not visible within the specified timeout. Exception: {ex.Message}");
            return false;
        }

        return false;
    }

    public async Task<string> GetTextByXPathAsync(string xpath)
    {
        try
        {
            await VerifyElementVisibleAndEnable(xpath);
            var element = await GetLocator(xpath).ElementHandleAsync();

            var text = await element.InnerTextAsync();
            Log.Information($"Successfully retrieved text from XPath '{xpath}': '{text}'");
            return text;
        }
        catch (PlaywrightException ex)
        {
            Log.Error($"An error occurred while trying to get text from '{xpath}': {ex.Message}");
            return string.Empty;
        }
    }

    protected async Task<bool> WaitForTextPresence(string text)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await Page.WaitForSelectorAsync($"text=\"{text}\"", new PageWaitForSelectorOptions { Timeout = 20000 });
            stopwatch.Stop();
            Log.Information($"Text: '{text}' found. Condition met after {stopwatch.ElapsedMilliseconds}ms.");
            return true;
        }
        catch (TimeoutException)
        {
            stopwatch.Stop();
            Log.Information(
                $"Text: '{text}' not found within the timeout period of {stopwatch.ElapsedMilliseconds}ms.");
            return false;
        }
    }

    protected async Task<bool> WaitForTextAbsence(string text)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await Page.Locator($"text=\"{text}\"").WaitForAsync(new LocatorWaitForOptions
                { State = WaitForSelectorState.Hidden, Timeout = 5000 });
            stopwatch.Stop();
            Log.Information(
                $"Text: '{text}' is not present on the page. Condition met after {stopwatch.ElapsedMilliseconds}ms.");
            return true;
        }
        catch (TimeoutException)
        {
            stopwatch.Stop();
            Log.Information($"Text: '{text}' still present after waiting {stopwatch.ElapsedMilliseconds}ms.");
            return false;
        }
    }
}