using System.Diagnostics;
using System.Text;
using Allure.Net.Commons;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Serilog;
using Serilog.Events;
using test.playwright.framework.fixtures.auth;
using test.playwright.framework.fixtures.config;
using test.playwright.framework.pages.enums;
using test.playwright.framework.security.sql;
using test.playwright.framework.security.xss;
using test.playwright.framework.utils;
using test.playwright.framework.utils.diagnostics;
using test.playwright.framework.utils.interfaces;

namespace test.playwright.framework.base_abstract;

[TestFixture]
public abstract class BaseTest
{
     /* ---------- static DI container ---------- */
    public static IServiceProvider? Services;

    /* ---------- per‑class fields ---------- */
    private readonly TestMetricsManager _testMetricsManager;
    protected internal TestLifeCycleManager TestLifeCycleManager = null!;
    protected readonly AuthManager AuthManager;

    /* new capturer / store */
    private readonly IScreenCapturer _capturer;
    private readonly IArtifactStore _artifacts;
    private IVideoCopier _videoCopier;
    private readonly IImageComparer _comparer;

    protected IPage Page => TestLifeCycleManager.Page;
    private Stopwatch? _suiteStopwatch;
    private Stopwatch? _testStopwatch;
    private DateTime _testStartTime;
    protected static int TotalTestsRan;
    protected static int PassedTests;
    protected static int FailedTests;

    protected readonly AtfConfig Config;
    protected readonly AllureLifecycle Allure;
    private readonly XssTestReport _xssReport = new();
    private readonly SqlTestReport _sqlReport = new();
    private const string DefaultDialogAction = "Accept";

    protected BaseTest()
    {
        /*to run tests in a different env: AtfConfig.TestEnv.Override = "dev";*/
        Services ??= BuildServices();
        Config = Services.GetRequiredService<AtfConfig>();
        _capturer = Services.GetRequiredService<IScreenCapturer>();
        _artifacts = Services.GetRequiredService<IArtifactStore>();
        _videoCopier = Services.GetRequiredService<IVideoCopier>();
        _comparer = Services.GetRequiredService<IImageComparer>();
        Contracts.IProfileProvider profiles = Config;
        AuthManager = new AuthManager(profiles, Config);
        _testMetricsManager = new TestMetricsManager();
        Allure = AllureLifecycle.Instance;
    }

    /* ---------- DI bootstrap ---------- */
    private static IServiceProvider BuildServices()
    {
        var cfg = AtfConfig.ReadConfig();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
            .WriteTo.File("logs/consoleLogs.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var sc = new ServiceCollection();

        sc.AddSingleton(cfg);
        sc.AddSingleton<ILogger>(_ => Log.Logger);
        sc.AddSingleton<IPlaywright>(_ => Playwright.CreateAsync().GetAwaiter().GetResult());

        sc.AddSingleton<BrowserFactory, BrowserFactory>(sp => new BrowserFactory(
            sp.GetRequiredService<IPlaywright>(),
            Enum.Parse<SupportedBrowser>(cfg.Browser, true),
            sp.GetRequiredService<ILogger>(),
            cfg.Headless));

        sc.AddTransient<IScreenCapturer, LocalScreenCapturer>();
        sc.AddTransient<IArtifactStore>(_ => new FileSystemArtifactStore(cfg.ScreenshotPath!, Log.Logger));
        sc.AddTransient<IVideoCopier>(_ => new VideoCopier(Log.Logger));
        sc.AddTransient<IImageComparer>(_ => new PixelImageComparer());

        return sc.BuildServiceProvider();
    }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _suiteStopwatch = Stopwatch.StartNew();
        _testStartTime = DateTime.Now;

        Log.Information("Test Suite Started {Start}", _testStartTime);
        _testMetricsManager.TestCompleted += outcome => Log.Information("Test outcome: {Outcome}", outcome);
    }

    private async void OnDialogHandled(object? sender, IDialog dialog)
    {
        Log.Information($"Dialog triggered with message: '{dialog.Message}'");
        if (DefaultDialogAction.Equals("Accept", StringComparison.OrdinalIgnoreCase))
        {
            await dialog.AcceptAsync();
            Log.Information("Dialog accepted automatically.");
        }
        else
        {
            await dialog.DismissAsync();
        }
    }

    [SetUp]
    public async Task TestInit()
    {
        TotalTestsRan++;
        _testStopwatch = Stopwatch.StartNew();

        var lifeCycle = new TestLifeCycleManager(Services!.GetRequiredService<BrowserFactory>(), Config);
        TestLifeCycleManager = lifeCycle;
        await TestLifeCycleManager.InitializeTestAsync();
        TestLifeCycleManager.Page.Dialog += OnDialogHandled;
    }

    [TearDown]
    public async Task TestCleanup()
    {
        var testName = TestContext.CurrentContext.Test.FullName;
        Log.Information("Starting teardown for: {Test}", testName);

        try
        {
            var status = TestContext.CurrentContext.Result.Outcome.Status;
            if (status == TestStatus.Passed)
            {
                PassedTests++;
                _testMetricsManager.OnTestCompleted("Passed");
                Log.Information($"Test passed: {testName}");
            }

            else
            {
                FailedTests++;
                _testMetricsManager.OnTestCompleted("Failed");
                Log.Warning($"Test failed: {testName}");

                await CaptureFailureScreenshot("FailedTest");

                // — attach any retry screenshots —
                if (TestContext.CurrentContext.Test.Properties.Get("RetryScreenshots")
                    is List<(int attempt, byte[] buffer)> shots)
                {
                    foreach (var (attempt, buf) in shots)
                        await SaveAndAttach(buf, $"RetryAttempt_{attempt}");
                }

                /*when we need to record test: _videoCopier.CopyLastVideo("videos/", "failedTests/", testName);*/
            }

            // — attach retry logs —
            if (TestContext.CurrentContext.Test.Properties.Get("RetryLogs") is List<string> logs)
            {
                var combined = string.Join(Environment.NewLine, logs);
                _artifacts.AttachToReport(Encoding.UTF8.GetBytes(combined), "Retry Logs");
            }
        }
        finally
        {
            var shouldSkipClose =
                TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed &&
                TestContext.CurrentContext.Test.Properties.Get("RemainingRetries") is int remaining &&
                remaining > 0;
            if (shouldSkipClose)
            {
                Log.Information("Skipping CloseTestAsync because 'Remaining' retry attempts remain.");
            }
            else
            {
                TestLifeCycleManager.Page.Dialog -= OnDialogHandled;
                await TestLifeCycleManager.DisposeAsync();
                Log.Information("Closed browser context.");
            }

            _testStopwatch!.Stop();
            Log.Information("Test finished ({Sec:n1}s)", _testStopwatch.Elapsed.TotalSeconds);
            Log.Information("Totals so far — Run:{Run}, Passed:{Pass}, Failed:{Fail}",
                TotalTestsRan, PassedTests, FailedTests);
        }
    }

    /* ---------- suite teardown ---------- */
    [OneTimeTearDown]
    public async Task SuiteCleanup()
    {
        _testMetricsManager.LogMetrics();
        _suiteStopwatch!.Stop();

        Log.Information("Test Suite Completed ({Sec:n1}s)", _suiteStopwatch.Elapsed.TotalSeconds);

        if (_xssReport.TotalFieldsTested > 0)
            await WriteJsonReport("reports/XssTestSummary.json", _xssReport);

        if (_sqlReport.TotalFieldsTested > 0)
            await WriteJsonReport("reports/SqlInjectionTestSummary.json", _sqlReport);
    }

    /* ---------- helper methods ---------- */
    private async Task CaptureFailureScreenshot(string prefix)
    {
        var png = await _capturer.CaptureAsync(Page);
        await SaveAndAttach(png, prefix);
    }

    private async Task SaveAndAttach(byte[] png, string prefix)
    {
        await _artifacts.SaveScreenshotAsync(png, prefix);
        _artifacts.AttachToReport(png, prefix);
    }

    private static async Task WriteJsonReport(string path, object obj)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonConvert.SerializeObject(obj, Formatting.Indented));
        Log.Information("Report saved: {Path}", path);
    }

    /* ---------- convenience for derived tests ---------- */
    protected static async Task CaptureAllureScreenshot(IPage page)
    {
        var capturer = Services!.GetRequiredService<IScreenCapturer>();
        var artifacts = Services!.GetRequiredService<IArtifactStore>();

        var png = await capturer.CaptureAsync(page);
        artifacts.AttachToReport(png, "Screenshot");
    }

    /* ---------- aggregate‑report helpers ---------- */
    protected void UpdateGlobalXssReport(XssTestReport r) => Aggregate(_xssReport, r);
    protected void UpdateSqlInjectionReport(SqlTestReport r) => Aggregate(_sqlReport, r);

    private static void Aggregate(dynamic agg, dynamic delta)
    {
        agg.TotalPayloadsTested += delta.TotalPayloadsTested;
        agg.TotalFieldsTested += delta.TotalFieldsTested;
        agg.TotalPassed += delta.TotalPassed;
        agg.TotalFailed += delta.TotalFailed;
        agg.TestDetails.AddRange(delta.TestDetails);
    }
}