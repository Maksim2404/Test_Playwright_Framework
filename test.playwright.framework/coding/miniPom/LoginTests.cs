using NUnit.Framework;

namespace test.playwright.framework.coding.miniPom;

public class LoginTests : BaseTest
{
    [Test]
    [Category("Smoke")]
    public async Task SuccessfulLoginTest()
    {
        var loginPage = new LoginPage(Page);
        await Page.GotoAsync("https://example.com/login");
        await loginPage.LoginAsync("myUsername", "myPassword");

        Assert.IsTrue(Page.Url.Contains("/dashboard"));
    }

    [Test]
    [Category("Smoke")]
    public async Task ChainPageActions_SuccessfulLoginTest()
    {
        var loginPage = new LoginPage(Page);
        await Page.GotoAsync("https://example.com/login");

        await loginPage.FillUsername("myUsername");
        await loginPage.FillPassword("myPassword");
        await loginPage.ClickLogin();

        Assert.IsTrue(Page.Url.Contains("/dashboard"));
    }
}