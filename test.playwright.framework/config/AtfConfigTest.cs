using FluentAssertions;
using NUnit.Framework;

namespace test.playwright.framework.config;

public class AtfConfigTest
{
    [Test]
    public void CanReadConfigFile()
    {
        var appConfig = AtfConfig.ReadConfig();
        appConfig.AppUrl.Should().NotBeNullOrWhiteSpace();
        appConfig.ScreenshotPath.Should().NotBeNullOrWhiteSpace();
    }
}