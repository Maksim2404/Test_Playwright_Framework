using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.base_abstract;

namespace test.playwright.framework.dataDrivenTests.pages;

public class LoginPage(IPage page) : BaseProjectElements(page)
{
    private const string UsernameField = "//form//following-sibling::input[@id='username']";
    private const string PasswordField = "//form//following-sibling::input[@id='password']";
    private const string LoginButton = "//form//following-sibling::button[@type='submit'][text()='Sign in']";

    /// <summary>
    /// Logs in with the provided credentials.
    /// </summary>
    /// <param name="email">The email or username.</param>
    /// <param name="password">The password.</param>
    public async Task LoginAsync(string email, string password)
    {
        try
        {
            await VerifyElementVisibleAndEnable(UsernameField);
            await Input(UsernameField, email);

            await VerifyElementVisibleAndEnable(PasswordField);
            await Input(PasswordField, password);

            await VerifyElementVisibleAndEnable(LoginButton);
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