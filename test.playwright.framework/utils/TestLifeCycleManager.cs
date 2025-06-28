using System.Diagnostics;
using Microsoft.Playwright;
using NUnit.Framework;
using Serilog;
using test.playwright.framework.auth;
using test.playwright.framework.config;

namespace test.playwright.framework.utils;

public class TestLifeCycleManager
{
    private readonly BrowserManager _browserMgr;
    private readonly AuthManager _authMgr;
    private readonly Contracts.IProfileProvider _profiles;
    public IPage Page { get; private set; } = null!;
    private IBrowserContext _browserContext = null!;
    public UserProfiles CurrentUserProfile { get; private set; } = null!;

    public TestLifeCycleManager(BrowserManager browserMgr, AtfConfig cfg)
    {
        _browserMgr = browserMgr;
        _profiles = cfg;
        _authMgr = new AuthManager(_profiles, cfg);
    }

    public async Task InitializeTestAsync()
    {
        try
        {
            Log.Information("Initializing Playwright...");
            await _browserMgr.InitializePlaywrightAsync();

            Log.Information("Creating browser context...");
            _browserContext = await _browserMgr.CreateIsolatedBrowserContextAsync();

            Log.Information("Creating new page...");
            Page = await _browserMgr.CreateNewPageAsync(_browserContext);

            Stopwatch.StartNew();
            Log.Information("Starting test: {TestName}", TestContext.CurrentContext.Test.FullName);
            Log.Information($"Test started at: {DateTime.Now}");

            //Skip auto-login if the attribute is present
            if (TestContext.CurrentContext.Test.Properties.ContainsKey("NoAutoLogin"))
            {
                Log.Information("⤷ Skipping auto-login (NoAutoLogin attribute).");
                return;
            }

            //Choose the profile
            //[TestCase("Bob")] overrides default
            var requestedName = TestContext.CurrentContext.Test.Arguments
                .OfType<string>()
                .FirstOrDefault();

            var profile = string.IsNullOrEmpty(requestedName)
                ? _profiles.GetByName("QA") // whatever you call your main user
                : _profiles.GetByName(requestedName);

            var mode = string.IsNullOrWhiteSpace(profile.TotpSecret)
                ? Contracts.LoginMode.PasswordOnly
                : Contracts.LoginMode.Totp;

            CurrentUserProfile = profile;
            await _authMgr.LoginAsync(Page, new Contracts.LoginRequest(profile, mode));
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
        await _browserMgr.CloseBrowserAsync();
    }

    public async Task SimulateOfflineModeAsync(bool isOffline)
    {
        Log.Information("Setting browser offline mode to: {OfflineState}", isOffline ? "ON" : "OFF");
        await _browserMgr.SetOfflineModeAsync(_browserContext, isOffline);

        Log.Information("Browser offline mode is now set to: {State}", isOffline ? "Offline" : "Online");
    }

    public async Task SimulateNetworkThrottlingAsync(string networkType)
    {
        await _browserMgr.SimulateNetworkThrottlingAsync(_browserContext, networkType);
    }
}