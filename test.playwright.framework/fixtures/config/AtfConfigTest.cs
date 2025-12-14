using FluentAssertions;
using NUnit.Framework;

namespace test.playwright.framework.fixtures.config;

public class AtfConfigTest
{
    [Test]
    public void CanReadConfig()
    {
        var appConfig = AtfConfig.ReadConfig();

        appConfig.AppUrl.Should().NotBeNullOrWhiteSpace()
            .And.StartWith("https://")
            .And.EndWith("/");

        appConfig.UserProfiles.Should().NotBeEmpty();
        foreach (var userProfile in appConfig.UserProfiles)
        {
            userProfile.Name.Should().NotBeNullOrWhiteSpace();
            userProfile.UserName.Should().NotBeNullOrWhiteSpace();
            userProfile.Password.Should().NotBeNullOrWhiteSpace();
        }

        appConfig.ScreenshotPath.Should().NotBeNullOrWhiteSpace();
    }
}