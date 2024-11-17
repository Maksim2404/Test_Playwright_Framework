using System.Diagnostics;
using Microsoft.Playwright;
using NUnit.Framework;
using Serilog;
using test.playwright.framework.config;

namespace test.playwright.framework.utils;

public class TestLifeCycleManager(BrowserManager browserManager)
{
    public IPage Page { get; private set; }
    private IBrowserContext _browserContext = null!;
    private AtfConfig _atfConfig;

    private ILocator GetLocator(string selector)
    {
        return selector.StartsWith("xpath=") ? Page.Locator(selector[6..]) : Page.Locator(selector);
    }

    public async Task WaitForNetworkIdle()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
        Log.Information("Page reached 'NetworkIdle' state");
    }

    private async Task WaitForDomContentLoaded()
    {
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded,
            new PageWaitForLoadStateOptions { Timeout = 15000 });
    }

    private async Task<bool> VerifyElementVisibleAndEnable(string selector)
    {
        try
        {
            var locator = GetLocator(selector);
            await Page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = 15000 });
            await locator.WaitForAsync(new LocatorWaitForOptions
                { State = WaitForSelectorState.Visible, Timeout = 15000 });

            var isVisible = await Page.Locator(selector).IsVisibleAsync();
            var isEnabled = await Page.Locator(selector).IsEnabledAsync();
            return isVisible && isEnabled;
        }
        catch (PlaywrightException ex)
        {
            Log.Error($"Failed to verify if element with selector '{selector}' is visible and enabled: {ex.Message}");
        }

        return false;
    }

    private async Task<bool> WaitForSelectorToExistAsync(string selector, bool expectToExist = true)
    {
        await WaitForNetworkIdle();
        await WaitForDomContentLoaded();

        try
        {
            var elementHandle = await Page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
            {
                State = expectToExist ? WaitForSelectorState.Attached : WaitForSelectorState.Detached,
                Timeout = expectToExist ? 20000 : 1000
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

    private async Task<string> GetTitle()
    {
        var title = await Page.TitleAsync();
        Log.Information($"Page title retrieved: {title}");
        return title;
    }

    private async Task OpenBaseUrl()
    {
        if (_atfConfig.AppUrl != null)
            await Page.GotoAsync(_atfConfig.AppUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }

    public async Task InitializeTestAsync()
    {
        try
        {
            await browserManager.InitializePlaywrightAsync();
            _browserContext = await browserManager.CreateIsolatedBrowserContextAsync();
            Page = await browserManager.CreateNewPageAsync(_browserContext);

            Stopwatch.StartNew();
            Log.Information("Starting test: {TestName}", TestContext.CurrentContext.Test.FullName);
            Log.Information($"Test started at: {DateTime.Now}");

            _atfConfig = AtfConfig.ReadConfig();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during test setup");
            throw;
        }
    }

    public async Task CloseTestAsync()
    {
        await _browserContext.CloseAsync();
        await browserManager.CloseBrowserAsync();
    }
}