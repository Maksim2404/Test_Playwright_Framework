using Microsoft.Extensions.Configuration;
using test.playwright.framework.base_abstract;

namespace test.playwright.framework.config;

public class AtfConfig
{
    public string? AppUrl { get; set; }
    public string? ScreenshotPath { get; private set; }

    public static AtfConfig ReadConfig()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<BaseTest>()
            .AddEnvironmentVariables()
            .Build();

        var atfConfig = config.GetRequiredSection("test:playwright").Get<AtfConfig>();
        atfConfig!.ScreenshotPath = config["test:playwright:ScreenshotPath"];

        return atfConfig;
    }
}