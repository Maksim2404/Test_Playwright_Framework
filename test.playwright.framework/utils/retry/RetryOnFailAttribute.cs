using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using Serilog;
using test.playwright.framework.base_abstract;
using test.playwright.framework.utils.interfaces;

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

    private class RetryCommand(TestCommand innerCommand, int maxRetries) : DelegatingTestCommand(innerCommand)
    {
        public override TestResult Execute(TestExecutionContext context)
        {
            var attemptScreenshots = new List<(int attempt, byte[] png)>();
            var attemptLogs = new List<string>();

            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                var remaining = maxRetries - attempt;
                context.CurrentTest.Properties.Set("RemainingRetries", remaining);
                innerCommand.Execute(context);

                if (context.CurrentResult.ResultState.Status != TestStatus.Failed)
                    break;

                /* -------- capture screenshot on failure -------- */
                if (context.TestObject is BaseTest baseTest)
                {
                    try
                    {
                        var page = baseTest.TestLifeCycleManager.Page;
                        var services = BaseTest.Services;
                        var capturer = services!.GetRequiredService<IScreenCapturer>();
                        var artifacts = services!.GetRequiredService<IArtifactStore>();

                        if (!page.IsClosed)
                        {
                            var png = capturer.CaptureAsync(page).GetAwaiter().GetResult();
                            if (png.Length > 0)
                            {
                                attemptScreenshots.Add((attempt, png));

                                artifacts.AttachToReport(png, $"Retry attempt {attempt}");
                                artifacts.SaveScreenshotAsync(png, $"{context.CurrentTest.Name}_retry{attempt}")
                                    .GetAwaiter()
                                    .GetResult();

                                Log.Information("RetryOnFail captured screenshot for attempt #{Attempt} ({Test})",
                                    attempt, context.CurrentTest.Name);
                            }
                        }
                        else
                        {
                            Log.Warning("Page already closed—no screenshot for attempt {Attempt}", attempt);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message, "RetryOnFail: error capturing screenshot");
                    }
                }

                attemptLogs.Add(
                    $"[Attempt {attempt}] Failure: {context.CurrentResult.Message}");

                if (attempt < maxRetries) context.CurrentResult = context.CurrentTest.MakeTestResult();
            }

            if (attemptLogs.Count > 0) context.CurrentTest.Properties.Set("RetryLogs", attemptLogs);
            if (attemptScreenshots.Count > 0)
                context.CurrentTest.Properties.Set("RetryScreenshots", attemptScreenshots);

            return context.CurrentResult;
        }
    }
}