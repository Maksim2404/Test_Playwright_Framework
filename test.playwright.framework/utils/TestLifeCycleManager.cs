using System.Diagnostics;
using Microsoft.Playwright;
using NUnit.Framework;
using Serilog;
using test.playwright.framework.config;

namespace test.playwright.framework.utils;

public class TestLifeCycleManager(BrowserManager browserManager, AtfConfig atfConfig)
{
    public IPage Page { get; private set; } = null!;
    private IBrowserContext _browserContext = null!;
    private AtfConfig _atfConfig = atfConfig;

    private async Task OpenBaseUrl()
    {
        if (_atfConfig.AppUrl != null)
            await Page.GotoAsync(_atfConfig.AppUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }

    public async Task InitializeTestAsync()
    {
        try
        {
            Log.Information("Initializing Playwright...");
            await browserManager.InitializePlaywrightAsync();

            Log.Information("Creating browser context...");
            _browserContext = await browserManager.CreateIsolatedBrowserContextAsync();

            Log.Information("Creating new page...");
            Page = await browserManager.CreateNewPageAsync(_browserContext);

            Stopwatch.StartNew();
            Log.Information("Starting test: {TestName}", TestContext.CurrentContext.Test.FullName);
            Log.Information($"Test started at: {DateTime.Now}");

            _atfConfig = AtfConfig.ReadConfig();

            await OpenBaseUrl();
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

    public async Task SimulateOfflineModeAsync(bool isOffline)
    {
        Log.Information("Setting browser offline mode to: {OfflineState}", isOffline ? "ON" : "OFF");
        await browserManager.SetOfflineModeAsync(_browserContext, isOffline);

        Log.Information("Browser offline mode is now set to: {State}", isOffline ? "Offline" : "Online");
    }

    public async Task SimulateNetworkThrottlingAsync(string networkType)
    {
        await browserManager.SimulateNetworkThrottlingAsync(_browserContext, networkType);
    }
}