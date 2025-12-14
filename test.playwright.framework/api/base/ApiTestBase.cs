using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Serilog;
using Serilog.Events;
using test.playwright.framework.api.auth;
using test.playwright.framework.api.common;
using test.playwright.framework.api.config;
using test.playwright.framework.base_abstract;

namespace test.playwright.framework.api.@base;

public abstract class ApiTestBase
{
    protected HttpClient Http { get; private set; } = null!;
    protected ApiConfig ApiCfg { get; private set; } = null!;
    protected TokenProvider TokenProvider { get; private set; } = null!;
    private static bool _loggingInitialized;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var resultsDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "allure-results");

        if (!_loggingInitialized)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
                .WriteTo.File("logs/api_logs.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            _loggingInitialized = true;
        }

        var config = new ConfigurationBuilder()
            .AddUserSecrets<BaseTest>()
            .AddEnvironmentVariables()
            .Build();
        
        AllureEnvironmentWriter.Write(resultsDir,
            ("Project", "test.playwright.framework"),
            ("Layer", "API"),
            ("Env", ApiConfig.Read(config).env),
            ("OS", Environment.OSVersion.ToString()),
            ("DotNet", Environment.Version.ToString())
        );

        var (apiCfg, _, _) = ApiConfig.Read(config);
        ApiCfg = apiCfg;

        Http = new HttpClient
        {
            BaseAddress = new Uri(ApiCfg.ApiBaseUrl)
        };

        TokenProvider = new TokenProvider(new HttpClient(), ApiCfg);
    }
}