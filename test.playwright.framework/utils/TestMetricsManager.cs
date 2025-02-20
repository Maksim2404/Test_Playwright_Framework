﻿namespace test.playwright.framework.utils;

public class TestMetricsManager
{
    private int TotalTestsRan { get; set; }
    private int PassedTests { get; set; }
    private int FailedTests { get; set; }

    public event Action<string>? TestCompleted;

    public void OnTestCompleted(string finalOutcome)
    {
        switch (finalOutcome)
        {
            case "Passed":
                PassedTests++;
                break;
            case "Failed":
                FailedTests++;
                break;
        }

        TotalTestsRan++;
        TestCompleted?.Invoke(finalOutcome);
    }

    public void LogMetrics()
    {
        Console.WriteLine($"Total Tests: {TotalTestsRan}, Passed: {PassedTests}, Failed: {FailedTests}");
    }
}