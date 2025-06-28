using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Serilog;

namespace test.playwright.framework.utils;

public class BrowserManager
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;

    private readonly ILogger _networkLogger = new LoggerConfiguration()
        .MinimumLevel.Error()
        .WriteTo.File("logs/networkLogs.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();

    public async Task InitializePlaywrightAsync()
    {
        _playwright = await Playwright.CreateAsync();
        var browserType = GetBrowserType();

        _browser = browserType switch
        {
            BrowserType.Chromium => await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                { Headless = false }),
            BrowserType.Firefox => await _playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
                { Headless = false }),
            BrowserType.Webkit => await _playwright.Webkit.LaunchAsync(
                new BrowserTypeLaunchOptions { Headless = false }),
            _ => _browser
        };
    }

    public async Task CloseBrowserAsync()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }

    public async Task<IBrowserContext> CreateIsolatedBrowserContextAsync()
    {
        var browserContext = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true,
            /*RecordVideoDir = "videos/",*/
        });

        return browserContext;
    }

    public async Task<IPage> CreateNewPageAsync(IBrowserContext browserContext)
    {
        var page = await browserContext.NewPageAsync();
        AddNetworkEventListeners(page);
        return page;
    }

    private static string GetBrowserType()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("browserType.json")
            .Build();

        var browserType = configuration["BrowserType"];

        return browserType switch
        {
            "Firefox" => BrowserType.Firefox,
            "Chromium" => BrowserType.Chromium,
            "Webkit" => BrowserType.Webkit,
            _ => BrowserType.Chromium,
        };
    }

    private void AddNetworkEventListeners(IPage page)
    {
        page.Request += OnRequest;
        page.Response += OnResponse;
        page.RequestFailed += OnRequestFailed;
    }

    private void OnRequest(object? sender, IRequest request) =>
        _networkLogger.Information($"[Request] Method: {request.Method}, URL: {request.Url}");

    private void OnResponse(object? sender, IResponse response) =>
        _networkLogger.Information($"[Response] Status: {response.Status}, URL: {response.Url}");

    private void OnRequestFailed(object? sender, IRequest request) =>
        _networkLogger.Error($"[Request Failed] URL: {request.Url}");

    public async Task SimulateNetworkThrottlingAsync(IBrowserContext context, string networkType)
    {
        Log.Information("Applying network throttling: {NetworkType}", networkType);

        await context.UnrouteAsync("**/*");

        switch (networkType.ToLower())
        {
            case "slow3g":
                await context.RouteAsync("**/*", async route =>
                {
                    await Task.Delay(3000);
                    await route.ContinueAsync();
                });
                break;
            case "fast3g":
                await context.RouteAsync("**/*", async route =>
                {
                    await Task.Delay(500);
                    await route.ContinueAsync();
                });
                break;
            default:
                Log.Warning("Unknown network type: {NetworkType}", networkType);
                break;
        }

        Log.Information("Network throttling applied: {NetworkType}", networkType);
    }

    public async Task SetOfflineModeAsync(IBrowserContext context, bool isOffline)
    {
        await context.SetOfflineAsync(isOffline);
        _networkLogger.Information(isOffline ? "Browser set to offline mode" : "Browser set to online mode");
    }
}