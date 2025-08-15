using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using NUnit.Framework;
using Serilog;
using test.playwright.framework.base_abstract;
using test.playwright.framework.pages.enums;

namespace test.playwright.framework.utils;

public class BrowserFactory(IPlaywright playwright, SupportedBrowser browserType, ILogger log, bool headless)
{
    private IBrowser? _browser;

    public async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is not null) return _browser;

        _browser = browserType switch
        {
            SupportedBrowser.Firefox => await playwright.Firefox.LaunchAsync(DefaultLaunchOpts()),
            SupportedBrowser.Webkit => await playwright.Webkit.LaunchAsync(DefaultLaunchOpts()),
            _ => await playwright.Chromium.LaunchAsync(DefaultLaunchOpts()),
        };
        return _browser;
    }

    public async Task<IBrowserContext> CreateContextAsync(ViewportSize? viewport = null, bool ignoreHttps = true)
    {
        var browser = await GetBrowserAsync();
        var isFirefox = browserType == SupportedBrowser.Firefox;

        return await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = ignoreHttps,
            ViewportSize = viewport ?? new ViewportSize { Width = 1920, Height = 1080 },
            Permissions = isFirefox ? Array.Empty<string>() : ["clipboard-read", "clipboard-write"]
        });
    }

    public async Task<IPage> CreatePageAsync(IBrowserContext ctx, NetworkPreset preset = NetworkPreset.None)
    {
        var page = await ctx.NewPageAsync();
        page.Request += LogRequest;
        page.Response += LogResponse;
        page.RequestFailed += LogRequestFailed;

        if (preset != NetworkPreset.None)
            await ApplyThrottlingAsync(ctx, preset);

        return page;
    }

    public async Task SetOfflineAsync(IBrowserContext ctx, bool offline)
    {
        log.Information("Setting offline={Offline}", offline);
        await ctx.SetOfflineAsync(offline);
        log.Information(offline ? "Browser set to offline mode" : "Browser set to online mode");
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.CloseAsync();
        playwright.Dispose();
    }

    private async Task InternalApplyThrottlingAsync(IBrowserContext ctx, NetworkPreset preset)
    {
        if (preset == NetworkPreset.None) return;

        var delayMs = preset switch
        {
            NetworkPreset.Slow3G => 3_000,
            NetworkPreset.Fast3G => 500,
            _ => 0
        };

        log.Information("Applying {Preset} throttling ({Delay} ms)", preset, delayMs);
        await ctx.UnrouteAsync("**/*");

        await ctx.RouteAsync("**/*", async route =>
        {
            await Task.Delay(delayMs);
            await route.ContinueAsync();
        });
    }

    public Task ApplyThrottlingAsync(IBrowserContext ctx, NetworkPreset preset) => preset == NetworkPreset.None
        ? Task.CompletedTask
        : InternalApplyThrottlingAsync(ctx, preset);

    /* ---------- helpers ---------- */
    private BrowserTypeLaunchOptions DefaultLaunchOpts() => new() { Headless = headless };

    private void LogRequest(object? _, IRequest r) => log.Debug("[REQ] {Method} {Url}", r.Method, r.Url);
    private void LogResponse(object? _, IResponse r) => log.Debug("[RES] {Status} {Url}", r.Status, r.Url);
    private void LogRequestFailed(object? _, IRequest r) => log.Error("[FAIL] {Url}", r.Url);
}

[SetUpFixture]
public sealed class GlobalBrowserTeardown
{
    [OneTimeTearDown]
    public async Task CloseBrowser()
    {
        if (BaseTest.Services?.GetService<BrowserFactory>() is { } factory)
            await factory.DisposeAsync();

        await Log.CloseAndFlushAsync();
    }
}