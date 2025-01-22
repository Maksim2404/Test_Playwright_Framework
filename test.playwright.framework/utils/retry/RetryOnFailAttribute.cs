using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using Serilog;
using test.playwright.framework.base_abstract;

namespace test.playwright.framework.utils.retry;

/// <summary>
/// A custom Retry attribute that improves on NUnit's built-in [Retry()],
/// allowing a global config-based retry count and optional per-method override.
/// Integrates with Allure for attempt logging.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class RetryOnFailAttribute(int count = -1) : NUnitAttribute, IWrapSetUpTearDown
{
    private static readonly int GlobalRetryCount = RetryConfig.GlobalRetryCount;
    private readonly int _retryCount = (count == -1) ? GlobalRetryCount : count;

    public TestCommand Wrap(TestCommand command)
    {
        return new RetryCommand(command, _retryCount);
    }

    private class RetryCommand(TestCommand innerCommand, int retryCount) : DelegatingTestCommand(innerCommand)
    {
        public override TestResult Execute(TestExecutionContext context)
        {
            var attemptScreenshots = new List<(int attempt, byte[] buffer)>();
            var attemptLogs = new List<string>();

            for (var attempt = 1; attempt <= retryCount; attempt++)
            {
                innerCommand.Execute(context);

                if (context.CurrentResult.ResultState.Status != TestStatus.Failed)
                    break;

                if (context.TestObject is BaseTest baseTest)
                {
                    try
                    {
                        var page = baseTest.TestLifeCycleManager.Page;
                        var diagMgr = baseTest.DiagnosticManager;

                        var buffer = diagMgr.CaptureScreenshotBufferAsync(page)
                            .GetAwaiter().GetResult();

                        attemptScreenshots.Add((attempt, buffer));
                        context.CurrentTest.Properties.Set("RetryScreenshots", attemptScreenshots);

                        attemptLogs.Add(
                            $"[Attempt {attempt}] Test failed with message: {context.CurrentResult.Message}.");
                        context.CurrentTest.Properties.Set("RetryLogs", attemptLogs);

                        Log.Information(
                            $"RetryOnFail captured screenshot for attempt #{attempt} in {context.CurrentTest.Name}"
                        );
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error capturing screenshot in RetryOnFail: {ex.Message}");
                    }
                }

                if (attempt < retryCount)
                {
                    context.CurrentResult = context.CurrentTest.MakeTestResult();
                }
            }

            if (attemptLogs.Count > 0)
                context.CurrentTest.Properties.Set("RetryLogs", attemptLogs);


            if (attemptScreenshots.Count > 0)
                context.CurrentTest.Properties.Set("RetryScreenshots", attemptScreenshots);

            return context.CurrentResult;
        }
    }
}