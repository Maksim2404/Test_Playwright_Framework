using System.Diagnostics;
using Microsoft.Playwright;
using Serilog;

namespace test.playwright.framework.base_abstract;

public abstract class BasePage(IPage page)
{
    protected IPage Page { get; } = page ?? throw new ArgumentNullException(nameof(page), "Page cannot be null");

    protected async Task<IPage> OpenNewPageAsync(Func<Task> actionToOpenPage)
    {
        var newPageTask = Page.Context.WaitForPageAsync();

        await actionToOpenPage();

        var newPage = await newPageTask;
        await newPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return newPage;
    }

    protected internal async Task<IPage> CloseCurrentPageAndSwitchBackAsync()
    {
        await Page.CloseAsync();

        var allPages = Page.Context.Pages;
        var previousPage = allPages[^1];

        await previousPage.BringToFrontAsync(); // Ensure the previous page is in the foreground
        return previousPage;
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

    protected async Task<bool> IsElementReadyForInteraction(ILocator locator, int timeoutMs = 10000)
    {
        try
        {
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeoutMs
            });

            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                if (await locator.IsEnabledAsync())
                {
                    Log.Information($"Element '{locator}' is visible and enabled. Ready for interaction.");
                    return true;
                }

                await Task.Delay(250);
            }

            Log.Information($"Element '{locator}' is visible but stayed disabled up to {timeoutMs}ms.");
            return false;
        }
        catch (TimeoutException ex)
        {
            Log.Error($"Element '{locator}' not visible within {timeoutMs}ms: {ex.Message}");
            return false;
        }
        catch (PlaywrightException ex)
        {
            Log.Error($"Failed to verify if element '{locator}' is ready for interaction: {ex.Message}");
            return false;
        }
    }

    private async Task HoverToElement(ILocator locator)
    {
        if (!await IsElementReadyForInteraction(locator)) return;
        await locator.HoverAsync();
        Log.Information($"Hovered over element: {locator}");
    }

    protected async Task Click(ILocator locator)
    {
        try
        {
            if (await IsElementReadyForInteraction(locator))
            {
                await HoverToElement(locator);
                await locator.ClickAsync();
                Log.Information($"Clicked on element: {locator}");
            }
        }
        catch (PlaywrightException ex)
        {
            Log.Error($"Failed to click on element with locator '{locator}': {ex.Message}");
            throw;
        }
    }

    protected async Task ClickElementByMouse(ILocator locator)
    {
        if (!await IsElementReadyForInteraction(locator)) return;

        var boundingBox = await locator.BoundingBoxAsync();
        if (boundingBox != null)
        {
            var x = boundingBox.X + boundingBox.Width / 2;
            var y = boundingBox.Y + boundingBox.Height / 2;
            await Page.Mouse.ClickAsync(x, y);
            Log.Information($"Clicked on element {locator} by mouse!");
        }
        else
        {
            Log.Information("Element's bounding box is not available");
        }
    }

    protected async Task DoubleClickElement(ILocator locator)
    {
        try
        {
            await IsElementReadyForInteraction(locator);
            await locator.DblClickAsync();

            await WaitForNetworkIdle();
            Log.Information($"Double-clicked on element: {locator}");
        }
        catch (PlaywrightException ex)
        {
            Log.Error($"Failed to double-click on element with locator '{locator}': {ex.Message}");
            throw;
        }
    }

    public async Task<string> GetTitle()
    {
        var title = await Page.TitleAsync();
        Log.Information($"Page title retrieved: {title}");
        return title;
    }

    public async Task DragAndDrop(ILocator sourceLocator, ILocator targetLocator)
    {
        try
        {
            var isSourceVisibleAndEnabled = await IsElementReadyForInteraction(sourceLocator);
            var isTargetVisibleAndEnabled = await IsElementReadyForInteraction(targetLocator);

            if (isSourceVisibleAndEnabled && isTargetVisibleAndEnabled)
            {
                {
                    var sourceElement = sourceLocator.First;
                    {
                        var targetElement = targetLocator.First;

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

    protected async Task Clear(ILocator locator)
    {
        await IsElementReadyForInteraction(locator);
        await locator.FillAsync(string.Empty);
        await WaitForNetworkIdle();
        Log.Information($"Cleared content of element: {locator}");
    }

    protected async Task Input(ILocator locator, string text)
    {
        try
        {
            if (await IsElementReadyForInteraction(locator))
            {
                await locator.ClickAsync();
                await locator.ClearAsync();
                await locator.FillAsync(text);
                await WaitForNetworkIdle();
                Log.Information($"Filled text '{text}' into element: {locator}");
            }
        }
        catch (PlaywrightException ex)
        {
            Log.Error($"Failed to fill text '{text}' into element with locator '{locator}': {ex.Message}");
            throw;
        }
    }

    protected async Task InputByKeyboard(ILocator locator, string text)
    {
        await WaitForLocatorToExistAsync(locator);
        await Click(locator);
        await Page.Keyboard.TypeAsync(text, new KeyboardTypeOptions { Delay = 50 });
        await WaitForNetworkIdle();
        Log.Information($"Inputted text by keyboard '{text}' into element: {locator}");
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

    protected async Task<string> GetTextByXPathAsync(ILocator xpath)
    {
        try
        {
            await IsElementReadyForInteraction(xpath);
            var element = await xpath.ElementHandleAsync();

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

    protected async Task ClickEnterByKeyboard()
    {
        await Page.Keyboard.PressAsync("Enter");
        Log.Information("Clicked 'Enter' button by keyword");
        await WaitForNetworkIdle();
    }

    protected async Task<bool> ClickSaveShortcutByKeyboard(ILocator? successSelector = null)
    {
        try
        {
            await Page.Keyboard.PressAsync("Control+s");
            Log.Information("Pressed 'Save' shortcut (Ctrl+S) on the keyboard.");

            await WaitForNetworkIdle();

            if (successSelector != null) await WaitForLocatorToExistAsync(successSelector);
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

    protected internal async Task ClickEscapeByKeyboard()
    {
        await Page.Keyboard.PressAsync("Escape");
        Log.Information("Clicked 'Escape' button by keyword");
        await WaitForNetworkIdle();
    }

    protected internal async Task RefreshPage()
    {
        await Page.ReloadAsync();
        Log.Information("Page Refreshed");
        await WaitForNetworkIdle();
    }

    public async Task<bool> WaitForLocatorToExistAsync(ILocator locator, int timeoutMs = 15000)
    {
        try
        {
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeoutMs
            });

            Log.Information($"Locator {locator} became visible within {timeoutMs}ms.");
            return true;
        }
        catch (TimeoutException ex)
        {
            Log.Warning($"Timeout waiting for locator to appear - {ex.Message}");
            return false;
        }
        catch (PlaywrightException ex)
        {
            Log.Error($"Playwright error while waiting for locator: {ex.Message}");
            return false;
        }
    }

    protected async Task<bool> WaitUntilAbsentAsync(ILocator locator, int timeoutMs = 5_000)
    {
        try
        {
            await Task.WhenAny(
                locator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Detached,
                    Timeout = timeoutMs
                }),
                locator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Hidden,
                    Timeout = timeoutMs
                })
            );

            return await locator.CountAsync() == 0;
        }
        catch (TimeoutException)
        {
            Log.Error("Locator still present after timeout.");
            return false;
        }
    }
}