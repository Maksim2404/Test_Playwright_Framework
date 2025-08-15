using System.Diagnostics;
using Microsoft.Playwright;
using NUnit.Framework;
using Serilog;
using test.playwright.framework.auth;
using test.playwright.framework.config;
using test.playwright.framework.pages.enums;

namespace test.playwright.framework.utils;

public class TestLifeCycleManager : IAsyncDisposable
{
    private readonly BrowserFactory _browserFactory;
    private readonly AuthManager _authMgr;
    private readonly Contracts.IProfileProvider _profiles;
    public IPage Page { get; private set; } = null!;
    private IBrowserContext _browserContext = null!;
    public UserProfile CurrentUserProfile { get; private set; } = null!;
    private readonly Stopwatch _stopwatch = new();

    public TestLifeCycleManager(BrowserFactory browserFactory, AtfConfig cfg)
    {
        _browserFactory = browserFactory;
        _profiles = cfg;
        _authMgr = new AuthManager(_profiles, cfg);
    }

    public async Task InitializeTestAsync()
    {
        Log.Information("Creating Playwright context & page …");
        _browserContext = await _browserFactory.CreateContextAsync();
        Page = await _browserFactory.CreatePageAsync(_browserContext);

        _stopwatch.Start();
        Log.Information("🟢 Test started: {Name}", TestContext.CurrentContext.Test.FullName);

        //Skip auto-login if the attribute is present
        if (TestContext.CurrentContext.Test.Properties.ContainsKey("NoAutoLogin"))
        {
            Log.Information("⤷ Skipping auto-login (NoAutoLogin attribute).");
            return;
        }

        //Choose the profile by typing [TestCase("Bob")] overrides default
        var requestedName = TestContext.CurrentContext.Test.Arguments
            .OfType<string>()
            .FirstOrDefault();

        var profile = string.IsNullOrEmpty(requestedName)
            ? _profiles.GetByName("QA AM")
            : _profiles.GetByName(requestedName);

        var mode = string.IsNullOrWhiteSpace(profile.TotpSecret)
            ? Contracts.LoginMode.PasswordOnly
            : Contracts.LoginMode.Totp;

        CurrentUserProfile = profile;
        await _authMgr.LoginAsync(Page, new Contracts.LoginRequest(profile, mode));
    }

    /* ---------- network helpers ---------- */
    public Task SimulateOfflineModeAsync(bool offline) => _browserFactory.SetOfflineAsync(_browserContext, offline);

    public Task SimulateNetworkThrottlingAsync(NetworkPreset preset) =>
        _browserFactory.ApplyThrottlingAsync(_browserContext, preset);

    /* ---------- disposal ---------- */
    public async ValueTask DisposeAsync()
    {
        _stopwatch.Stop();
        Log.Information("🔴 Test finished. Duration: {Sec:n1}s", _stopwatch.Elapsed.TotalSeconds);

        await _browserContext.DisposeAsync();
    }
}