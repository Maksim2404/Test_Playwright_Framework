using System.Diagnostics;
using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.config;
using test.playwright.framework.constants;

namespace test.playwright.framework.auth;

public sealed class AuthManager(Contracts.IProfileProvider provider, AtfConfig cfg)
{
    private const string SignInSelector = "//a[@href and contains(text(), 'Sign in')]";
    private const string TotpInputFieldSelector = "//input[@id='otp']";

    private static async Task IsElementInteractable(ILocator locator, int timeoutMs = 10000)
    {
        try
        {
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeoutMs
            });

            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                if (await locator.IsEnabledAsync())
                {
                    Log.Information($"Element '{locator}' is visible and enabled. Ready for interaction.");
                    return;
                }

                await Task.Delay(250);
            }

            Log.Information($"Element '{locator}' is visible but stayed disabled up to {timeoutMs}ms.");
        }
        catch (TimeoutException ex)
        {
            Log.Error($"Element '{locator}' not visible within {timeoutMs}ms: {ex.Message}");
        }
        catch (PlaywrightException ex)
        {
            Log.Error($"Failed to verify if element '{locator}' is ready for interaction: {ex.Message}");
        }
    }

    private static async Task FillAndSubmitLoginForm(IPage page, UserProfiles p)
    {
        var userNameLocator = page.Locator(TestDataConstants.UserNameLocator);
        var passwordLocator = page.Locator(TestDataConstants.PasswordLocator);
        var loginButtonLocator = page.Locator(TestDataConstants.LoginButtonLocator);

        await IsElementInteractable(userNameLocator);
        await IsElementInteractable(passwordLocator);
        await IsElementInteractable(loginButtonLocator);

        await userNameLocator.FillAsync(p.UserName);
        await passwordLocator.FillAsync(p.Password);
        await loginButtonLocator.ClickAsync();

        Log.Information("Login action performed");
    }

    private async Task OpenBaseUrl(IPage page)
    {
        await page.GotoAsync(cfg.AppUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }

    public async Task LoginAsync(IPage page, Contracts.LoginRequest req)
    {
        Log.Debug($"Login with {req.Profile.Name} – password-only flow");
        await OpenBaseUrl(page);
        await FillAndSubmitLoginForm(page, req.Profile);

        if (req.Mode is Contracts.LoginMode.Totp)
        {
            Log.Debug($"Login with {req.Profile.Name} – TOTP flow");
            var otpInputField = page.Locator(TotpInputFieldSelector);
            await otpInputField.WaitForAsync();
            await TotpLogin.PerformAsync(page, req.Profile.TotpSecret!, otpInputField);
        }
    }

    public async Task SignInFromLandingAsync(IPage page, UserProfiles profile)
    {
        var locator = page.Locator(SignInSelector);
        await IsElementInteractable(locator);
        await locator.ClickAsync();

        await LoginAsync(page, new Contracts.LoginRequest(profile,
            string.IsNullOrWhiteSpace(profile.TotpSecret)
                ? Contracts.LoginMode.PasswordOnly
                : Contracts.LoginMode.Totp));
    }
}
