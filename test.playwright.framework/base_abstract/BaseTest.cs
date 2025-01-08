using System.Diagnostics;
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
    protected readonly AtfConfig Config;
    protected readonly AllureLifecycle Allure;
    private int _currentRetryCount;
    private bool _testPassed;
    private const int MaxRetries = 1;
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
        TestLifeCycleManager = new TestLifeCycleManager(browserManager);
        Config = AtfConfig.ReadConfig();
        _testMetricsManager = new TestMetricsManager();
        DiagnosticManager = new DiagnosticManager(Config);
        Allure = AllureLifecycle.Instance;
        _currentRetryCount = 0;
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
        TotalTestsRan++;
        _testStopwatch = Stopwatch.StartNew();
        _currentRetryCount = 0;
        _testPassed = false;
        await TestLifeCycleManager.InitializeTestAsync();
    }

    private async Task RetryTestAsync()
    {
        Log.Information($"Retrying test: Attempt {_currentRetryCount}/{MaxRetries}");

        try
        {
            await TestLifeCycleManager.CloseTestAsync();
            await TestLifeCycleManager.InitializeTestAsync();

            var testMethodName = TestContext.CurrentContext.Test.MethodName;
            if (testMethodName != null)
            {
                var testMethod = GetType().GetMethod(testMethodName);

                if (testMethod != null)
                {
                    var result = testMethod.Invoke(this, null);
                    if (result is Task taskResult)
                    {
                        await taskResult;
                    }

                    _testPassed = true;
                    return;
                }

                Log.Error($"Test method '{testMethodName}' could not be found.");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Retry failed for test {TestContext.CurrentContext.Test.FullName}: {ex.Message}");
        }
    }

    private void LogTestCompletion()
    {
        _testStopwatch?.Stop();
        var finalStatus = _testPassed ? "Passed" : "Failed";

        Log.Information($"Test status: {finalStatus}");
        Log.Information($"Test finished at: {DateTime.Now}");

        if (_testStopwatch != null) Log.Information($"Test execution time: {_testStopwatch.Elapsed}");
    }

    [TearDown]
    public async Task AfterMethod()
    {
        var testName = TestContext.CurrentContext.Test.FullName;

        try
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Passed || _testPassed)
            {
                _testPassed = true;
                PassedTests++;
                _testMetricsManager.OnTestCompleted("Passed");
                Log.Information($"Test passed on initial attempt: {testName}");
                return;
            }

            Log.Warning($"Initial test failed: {testName}");

            var screenshotPath =
                await DiagnosticManager.CaptureScreenshotAsync(Page, "FailedTest", includeTimestamp: true);

            if (!string.IsNullOrEmpty(screenshotPath))
            {
                await CaptureAllureScreenshot(Page);
            }

            DiagnosticManager.CaptureVideoOfFailedTest("videos/", "failedTests");

            if (_currentRetryCount < MaxRetries)
            {
                _currentRetryCount++;
                Log.Information($"Retrying test: {_currentRetryCount}/{MaxRetries}: {testName}");
                await RetryTestAsync();

                if (_testPassed)
                {
                    Log.Information($"Test passed on retry attempt {_currentRetryCount}: {testName}");
                    PassedTests++;
                    _testMetricsManager.OnTestCompleted("Passed");
                    return;
                }
            }

            FailedTests++;
            _testMetricsManager.OnTestCompleted("Failed");
            Log.Warning($"Test failed after retry: {testName}");
        }
        catch (Exception ex)
        {
            if (_testPassed)
            {
                Log.Information($"Retry succeeded for {testName}, suppressing initial failure: {ex.Message}");
            }
            else
            {
                Log.Error($"Unexpected error during teardown for {testName}: {ex.Message}");
                throw;
            }
        }
        finally
        {
            LogTestCompletion();
            Log.Information($"Tests Run So Far: {TotalTestsRan}, Passed: {PassedTests}, Failed: {FailedTests}");
            await TestLifeCycleManager.CloseTestAsync();

            if (_testPassed)
            {
                Log.Information($"Final status - Test passed: {testName}");
            }
        }
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

        if (_xssReport.TotalFieldsTested > 0)
        {
            const string xssReportPath = "reports/XssTestSummary.json";
            Directory.CreateDirectory("reports");
            File.WriteAllText(xssReportPath, JsonConvert.SerializeObject(_xssReport, Formatting.Indented));

            Log.Information($"XSS Test Summary Report saved at: {xssReportPath}");
            Console.WriteLine(JsonConvert.SerializeObject(_xssReport, Formatting.Indented));
        }

        if (_sqlInjectionReport.TotalFieldsTested <= 0) return;
        const string sqlReportPath = "reports/SqlInjectionTestSummary.json";
        Directory.CreateDirectory("reports");
        File.WriteAllText(sqlReportPath, JsonConvert.SerializeObject(_sqlInjectionReport, Formatting.Indented));

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