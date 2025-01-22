using System.Diagnostics;
using System.Text;
using Allure.Net.Commons;
using Microsoft.Playwright;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Serilog;
using test.playwright.framework.config;
using test.playwright.framework.security.sql;
using test.playwright.framework.security.xss;
using test.playwright.framework.utils;
using test.playwright.framework.utils.interfaces;

namespace test.playwright.framework.base_abstract;

public abstract class BaseTest
{
    private readonly TestMetricsManager _testMetricsManager;
    protected internal readonly IDiagnosticManager DiagnosticManager;
    protected internal readonly TestLifeCycleManager TestLifeCycleManager;
    protected IPage Page => TestLifeCycleManager.Page;
    private Stopwatch? _stopwatch;
    private DateTime _testStartTime;
    private Stopwatch? _testStopwatch;
    private static int _totalTestsRan;
    private static int _passedTests;
    private static int _failedTests;
    protected readonly AtfConfig Config;
    protected readonly AllureLifecycle Allure;
    private readonly XssTestReport _xssReport = new();
    private readonly SqlInjectionTestReport _sqlInjectionReport = new();

    protected void UpdateGlobalXssReport(XssTestReport xssReport)
    {
        _xssReport.TotalPayloadsTested += xssReport.TotalPayloadsTested;
        _xssReport.TotalFieldsTested += xssReport.TotalFieldsTested;
        _xssReport.TotalPassed += xssReport.TotalPassed;
        _xssReport.TotalFailed += xssReport.TotalFailed;
        _xssReport.TestDetails.AddRange(xssReport.TestDetails);
    }

    protected void UpdateSqlInjectionReport(SqlInjectionTestReport sqlReport)
    {
        _sqlInjectionReport.TotalPayloadsTested += sqlReport.TotalPayloadsTested;
        _sqlInjectionReport.TotalFieldsTested += sqlReport.TotalFieldsTested;
        _sqlInjectionReport.TotalPassed += sqlReport.TotalPassed;
        _sqlInjectionReport.TotalFailed += sqlReport.TotalFailed;
        _sqlInjectionReport.TestDetails.AddRange(sqlReport.TestDetails);
    }

    protected BaseTest()
    {
        var browserManager = new BrowserManager();
        Config = AtfConfig.ReadConfig();
        TestLifeCycleManager = new TestLifeCycleManager(browserManager, Config);
        _testMetricsManager = new TestMetricsManager();
        DiagnosticManager = new DiagnosticManager(Config);
        Allure = AllureLifecycle.Instance;
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

    [SetUp]
    public async Task BeforeMethod()
    {
        _totalTestsRan++;
        _testStopwatch = Stopwatch.StartNew();
        await TestLifeCycleManager.InitializeTestAsync();
    }

    [TearDown]
    public async Task AfterMethod()
    {
        var testName = TestContext.CurrentContext.Test.FullName;
        Log.Information($"Starting teardown for: {testName}");

        try
        {
            var status = TestContext.CurrentContext.Result.Outcome.Status;
            if (status == TestStatus.Passed)
            {
                _passedTests++;
                _testMetricsManager.OnTestCompleted("Passed");
                Log.Information($"Test passed: {testName}");
            }

            else
            {
                _failedTests++;
                _testMetricsManager.OnTestCompleted("Failed");
                Log.Warning($"Test failed: {testName}");

                var screenshotBuffer = await DiagnosticManager.CaptureScreenshotBufferAsync(TestLifeCycleManager.Page);
                if (screenshotBuffer is { Length: > 0 })
                {
                    await DiagnosticManager.SaveBufferToFileAsync(screenshotBuffer, "FailedTest",
                        includeTimestamp: true);
                    DiagnosticManager.AttachBufferToReport(screenshotBuffer);
                }
                else
                {
                    Log.Warning("Screenshot buffer was empty.");
                }
                /*DiagnosticsManager.CaptureVideoOfFailedTest("videos/", "failedTests");*/
            }

            var logsObj = TestContext.CurrentContext.Test.Properties.Get("RetryLogs");
            if (logsObj is List<string> { Count: > 0 } logs)
            {
                var combinedLogs = string.Join(Environment.NewLine, logs);
                var logBytes = Encoding.UTF8.GetBytes(combinedLogs);
                AllureApi.AddAttachment("Retry Logs", "text/plain", logBytes);
            }

            var screenshotsObj = TestContext.CurrentContext.Test.Properties.Get("RetryScreenshots");
            if (screenshotsObj is List<(int attempt, byte[] buffer)> { Count: > 0 } screenshotList)
            {
                Log.Information(
                    $"Found {screenshotList.Count} screenshot(s) from failed attempts in RetryOnFail logic.");

                foreach (var (attemptNumber, buffer) in screenshotList)
                {
                    await DiagnosticManager.SaveBufferToFileAsync(
                        buffer, $"RetryFailedTestAttempt_{attemptNumber}",
                        includeTimestamp: true
                    );
                    DiagnosticManager.AttachBufferToReport(buffer);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Unexpected error during teardown for {testName}: {ex.Message}");
            throw;
        }
        finally
        {
            _testStopwatch?.Stop();
            Log.Information($"Test finished at: {DateTime.Now}");
            if (_testStopwatch != null)
                Log.Information($"Test execution time: {_testStopwatch.Elapsed}");

            Log.Information($"Tests Run So Far: {_totalTestsRan}, Passed: {_passedTests}, Failed: {_failedTests}");
        }
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await TestLifeCycleManager.CloseTestAsync();
        _testMetricsManager.LogMetrics();
        Log.Information("Test Suite Completed");
        await Log.CloseAndFlushAsync();

        _stopwatch?.Stop();
        Log.Information(
            $"Final Metrics - Total tests run: {_totalTestsRan}, Passed: {_passedTests}, Failed: {_failedTests}");
        Console.WriteLine($"Test suite finished at: {DateTime.Now}");
        if (_stopwatch != null) Console.WriteLine($"Test execution time: {_stopwatch.Elapsed}");

        if (_xssReport.TotalFieldsTested > 0)
        {
            const string xssReportPath = "reports/XssTestSummary.json";
            Directory.CreateDirectory("reports");
            await File.WriteAllTextAsync(xssReportPath, JsonConvert.SerializeObject(_xssReport, Formatting.Indented));

            Log.Information($"XSS Test Summary Report saved at: {xssReportPath}");
            Console.WriteLine(JsonConvert.SerializeObject(_xssReport, Formatting.Indented));
        }

        if (_sqlInjectionReport.TotalFieldsTested <= 0) return;
        const string sqlReportPath = "reports/SqlInjectionTestSummary.json";
        Directory.CreateDirectory("reports");
        await File.WriteAllTextAsync(sqlReportPath,
            JsonConvert.SerializeObject(_sqlInjectionReport, Formatting.Indented));

        Log.Information($"SQL Injection Test Summary Report saved at: {sqlReportPath}");
        Console.WriteLine(JsonConvert.SerializeObject(_sqlInjectionReport, Formatting.Indented));
    }

    private static async Task CaptureAllureScreenshot(IPage page)
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