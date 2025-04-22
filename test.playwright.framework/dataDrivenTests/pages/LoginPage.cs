using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.base_abstract;

namespace test.playwright.framework.dataDrivenTests.pages;

public class LoginPage(IPage page) : BaseProjectElements(page)
{
    private ILocator UsernameField => Page.Locator("//form//following-sibling::input[@id='username']");
    private ILocator PasswordField => Page.Locator("//form//following-sibling::input[@id='password']");
    private ILocator LoginButton => Page.Locator("//form//following-sibling::button[@type='submit'][text()='Sign in']");

    /// <summary>
    /// Logs in with the provided credentials.
    /// </summary>
    /// <param name="email">The email or username.</param>
    /// <param name="password">The password.</param>
    public async Task LoginAsync(string email, string password)
    {
        try
        {
            await IsElementReadyForInteraction(UsernameField);
            await Input(UsernameField, email);

            await IsElementReadyForInteraction(PasswordField);
            await Input(PasswordField, password);

            await IsElementReadyForInteraction(LoginButton);
            await Click(LoginButton);

            await WaitForNetworkIdle();
            Log.Information($"Logged in successfully with email: {email}.");
        }
        catch (Exception ex)
        {
            Log.Error($"Login failed: {ex.Message}");
            throw;
        }
    }
}