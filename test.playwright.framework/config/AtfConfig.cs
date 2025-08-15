using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using test.playwright.framework.auth;
using test.playwright.framework.base_abstract;

namespace test.playwright.framework.config;

public sealed class AtfConfig : Contracts.IProfileProvider
{
    public required string AppUrl { get; set; } = null!;
    public required string Browser { get; set; } = "Chromium";
    public bool Headless { get; set; } = true;
    public required List<UserProfile> UserProfiles { get; init; } = [];
    public string? ScreenshotPath { get; private set; }

    public UserProfile GetByName(string name) =>
        UserProfiles.First(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static AtfConfig ReadConfig()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<BaseTest>()
            .AddEnvironmentVariables()
            .Build();

        /*when you need to switch between dev and qa envs: change the value inside "qa" to "dev"*/
        var envFromRun = TestContext.Parameters.Get("env");
        var envVar = config["TEST_ENV"];
        var env = (TestEnv.Override ?? envFromRun ?? envVar ?? "qa").Trim().ToLowerInvariant();
        var url = config[$"test:playwright:Urls:{env}"] ??
                  throw new InvalidOperationException($"No URL configured for env '{env}'");

        ValidateUrl(url);

        var atfConfig = config.GetRequiredSection("test:playwright").Get<AtfConfig>()!;
        atfConfig.Browser = config["TEST_BROWSER"] ?? atfConfig.Browser;
        atfConfig.Headless = bool.TryParse(config["PW_HEADLESS"], out var h) ? h : atfConfig.Headless;
        atfConfig.AppUrl = url.TrimEnd('/') + '/';
        atfConfig.ScreenshotPath = config["test:playwright:ScreenshotPath"];
        return atfConfig;
    }

    private static void ValidateUrl(string url)
    {
        if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only HTTPS base‑urls are allowed");
    }

    public static class TestEnv
    {
        public static string? Override;
    }
}