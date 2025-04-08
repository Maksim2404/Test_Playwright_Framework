using Microsoft.Playwright;

namespace test.playwright.framework.coding.miniPom;

public class LoginPage(IPage page) : BasePage(page)
{
    private ILocator UsernameInput => Page.Locator("#username");
    private ILocator PasswordInput => Page.Locator("#password");
    private ILocator LoginButton => Page.Locator("#login");

    public async Task LoginAsync(string username, string password)
    {
        await FillAsync(UsernameInput, username);
        await FillAsync(PasswordInput, password);
        await ClickAsync(LoginButton);
    }

    public async Task<LoginPage> FillUsername(string username)
    {
        await FillAsync(UsernameInput, username);
        return this;
    }

    public async Task<LoginPage> FillPassword(string password)
    {
        await FillAsync(PasswordInput, password);
        return this;
    }

    public async Task<LoginPage> ClickLogin()
    {
        await ClickAsync(LoginButton);
        return this;
    }
}