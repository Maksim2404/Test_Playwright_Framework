using Microsoft.Playwright;
using NUnit.Framework;
using Serilog;
using test.playwright.framework.utils;

namespace test.playwright.framework.dataDrivenTests.pages;

public abstract class DataDrivenBaseTest
{
    protected IPage Page { get; private set; } = null!;
    private IBrowserContext _browserContext = null!;
    private readonly BrowserManager _browserManager = new();
    private const string LoginUrl = "https://netlify.app/";

    /// <summary>
    /// Navigates to the login page.
    /// </summary>
    private async Task NavigateToLoginPageAsync()
    {
        try
        {
            await Page.GotoAsync(LoginUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            Log.Information("Navigated to login page.");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to navigate to login page: {ex.Message}");
            throw;
        }
    }

    [SetUp]
    public async Task SetUpAsync()
    {
        await _browserManager.InitializePlaywrightAsync();
        _browserContext = await _browserManager.CreateIsolatedBrowserContextAsync();
        Page = await _browserManager.CreateNewPageAsync(_browserContext);

        await NavigateToLoginPageAsync();
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        await _browserContext.CloseAsync();
        await _browserManager.CloseBrowserAsync();
    }
}