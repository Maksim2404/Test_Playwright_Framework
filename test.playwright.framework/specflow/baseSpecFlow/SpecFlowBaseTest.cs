using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Serilog;
using TechTalk.SpecFlow;
using test.playwright.framework.utils;
using BaseTest = test.playwright.framework.base_abstract.BaseTest;

namespace test.playwright.framework.specflow.baseSpecFlow;

public class SpecFlowBaseTest(TestMetricsManager testMetricsManager) : BaseTest
{
    [BeforeScenario]
    public async Task Setup()
    {
        await TestLifeCycleManager.InitializeTestAsync();
        TotalTestsRan++;
    }

    [AfterScenario]
    public async Task TearDown()
    {
        var testOutcome = TestContext.CurrentContext.Result.Outcome.Status.ToString();
        testMetricsManager.OnTestCompleted(testOutcome);

        if (TestContext.CurrentContext.Result.Outcome != ResultState.Success)
        {
        }

        Log.Information($"Tests Run So Far: {TotalTestsRan}, Passed: {PassedTests}, Failed: {FailedTests}");
        await TestLifeCycleManager.DisposeAsync();
    }
}