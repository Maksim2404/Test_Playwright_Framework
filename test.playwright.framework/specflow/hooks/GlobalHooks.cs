using System.Diagnostics;
using Serilog;
using TechTalk.SpecFlow;

namespace test.playwright.framework.specflow.hooks;


/*Ensuring that an instance of BaseTestBdd is available for injection into step definitions.
 This is crucial for enabling shared setup, such as browser initialization, across different step definition files*/

[Binding]
public class GlobalHooks
{
 private static Stopwatch? _stopwatch;
 private static DateTime _testStartTime;
    
 [BeforeTestRun]
 public static void BeforeTestRun()
 {
  Log.Logger = new LoggerConfiguration()
   .MinimumLevel.Debug()
   .WriteTo.Console()
   .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
   .CreateLogger();

  Log.Information("Test Suite Starting");

  _stopwatch = Stopwatch.StartNew();
  _testStartTime = DateTime.Now;
  Console.WriteLine($"Starting test suite: {_testStartTime}");
 }

 [AfterTestRun]
 public static void AfterTestRun()
 {
  Log.Information("Test Suite Completed");
  Log.CloseAndFlush();

  _stopwatch?.Stop();
  Console.WriteLine($"Test suite finished at: {DateTime.Now}");
  if (_stopwatch != null) Console.WriteLine($"Test execution time: {_stopwatch.Elapsed}");
 }
}