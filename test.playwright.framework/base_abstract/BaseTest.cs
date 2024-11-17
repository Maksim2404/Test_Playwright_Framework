using System.Diagnostics;
using Allure.Net.Commons;
using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Serilog;
using test.playwright.framework.config;
using test.playwright.framework.utils;

namespace test.playwright.framework.base_abstract;

public abstract class BaseTest
{
    private readonly TestMetricsManager _testMetricsManager;
    protected readonly DiagnosticManager DiagnosticManager;
    protected readonly TestLifeCycleManager TestLifeCycleManager;
    protected IPage Page => TestLifeCycleManager.Page;
    private Stopwatch? _stopwatch;
    private DateTime _testStartTime;
    private Stopwatch? _testStopwatch;
    protected static int TotalTestsRan;
    protected static int PassedTests;
    protected static int FailedTests;
    private static readonly Random Random = new();
    protected readonly AtfConfig Config;
    protected readonly AllureLifecycle Allure;

    protected BaseTest()
    {
        var browserManager = new BrowserManager();
        TestLifeCycleManager = new TestLifeCycleManager(browserManager);
        Config = AtfConfig.ReadConfig();
        _testMetricsManager = new TestMetricsManager();
        DiagnosticManager = new DiagnosticManager(Config);
        Allure = AllureLifecycle.Instance;
    }

    private async Task WaitForLoadState()
    {
        await Page.WaitForLoadStateAsync(LoadState.Load, new PageWaitForLoadStateOptions { Timeout = 15000 });
    }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        _stopwatch = Stopwatch.StartNew();
        _testStartTime = DateTime.Now;
        Log.Information($"Test Suite Started {_testStartTime}");
        _testMetricsManager.TestCompleted += outcome => Log.Information($"Test outcome: {outcome}");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _testMetricsManager.LogMetrics();
        Log.Information("Test Suite Completed");
        Log.CloseAndFlush();

        _stopwatch?.Stop();
        Log.Information(
            $"Final Metrics - Total tests run: {TotalTestsRan}, Passed: {PassedTests}, Failed: {FailedTests}");
        Console.WriteLine($"Test suite finished at: {DateTime.Now}");
        if (_stopwatch != null) Console.WriteLine($"Test execution time: {_stopwatch.Elapsed}");
    }

    [SetUp]
    public async Task BeforeMethod()
    {
        await TestLifeCycleManager.InitializeTestAsync();
        TotalTestsRan++;
        _testStopwatch = Stopwatch.StartNew();
    }

    protected void LogTestCompletion()
    {
        _testStopwatch?.Stop();
        Log.Information($"Test status: {TestContext.CurrentContext.Result.Outcome.Status}");
        Log.Information($"Test finished at: {DateTime.Now}");

        if (_testStopwatch != null) Log.Information($"Test execution time: {_testStopwatch.Elapsed}");

        UpdateTestCountersBasedOnOutcome();
    }

    private static void UpdateTestCountersBasedOnOutcome()
    {
        if (TestContext.CurrentContext.Result.Outcome == ResultState.Success)
        {
            PassedTests++;
        }
        else if (TestContext.CurrentContext.Result.Outcome == ResultState.Failure ||
                 TestContext.CurrentContext.Result.Outcome == ResultState.Error)
        {
            FailedTests++;
        }
    }

    [TearDown]
    public async Task AfterMethod()
    {
        var testOutcome = TestContext.CurrentContext.Result.Outcome.Status.ToString();
        _testMetricsManager.OnTestCompleted(testOutcome);

        if (TestContext.CurrentContext.Result.Outcome != ResultState.Success)
        {
            var screenshotPath =
                await DiagnosticManager.CaptureScreenshotAsync(Page, "FailedTest", includeTimestamp: true);

            if (!string.IsNullOrEmpty(screenshotPath))
            {
                await CaptureAllureScreenshot(Page);
            }

            DiagnosticManager.CaptureVideoOfFailedTest("videos/", "failedTests");
        }

        LogTestCompletion();
        Log.Information($"Tests Run So Far: {TotalTestsRan}, Passed: {PassedTests}, Failed: {FailedTests}");
        await TestLifeCycleManager.CloseTestAsync();
    }

    protected static async Task CaptureAllureScreenshot(IPage page)
    {
        try
        {
            const string screenshotDirectory = "allure-results/screenshots";
            Directory.CreateDirectory(screenshotDirectory);

            var fileName = $"AllureScreenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var screenshotPath = Path.Combine(screenshotDirectory, fileName);

            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = screenshotPath,
                FullPage = false
            });

            if (File.Exists(screenshotPath))
            {
                AllureApi.AddAttachment("Screenshot", "image/png", await File.ReadAllBytesAsync(screenshotPath));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error capturing screenshot: {ex.Message}");
        }
    }
}