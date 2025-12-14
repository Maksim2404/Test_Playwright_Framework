using System.Diagnostics;
using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.fixtures.config;
using test.playwright.framework.fixtures.constants;

namespace test.playwright.framework.fixtures.auth;

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
                    Log.Information("Element '{Locator}' is visible and enabled. Ready for interaction.", locator);
                    return;
                }

                await Task.Delay(250);
            }

            Log.Information("Element '{Locator}' is visible but stayed disabled up to {TimeoutMs}ms.", locator,
                timeoutMs);
        }
        catch (PlaywrightException ex)
        {
            Log.Error("Failed to verify if element '{Locator}' is ready for interaction: {ExMessage}", locator,
                ex.Message);
        }
    }

    private static async Task FillAndSubmitLoginForm(IPage page, UserProfile p)
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
        Log.Debug("Login with {ProfileName} – password-only flow", req.Profile.Name);
        await OpenBaseUrl(page);
        await FillAndSubmitLoginForm(page, req.Profile);

        if (req.Mode is Contracts.LoginMode.Totp)
        {
            Log.Debug("Login with {ProfileName} – TOTP flow", req.Profile.Name);
            var otpInputField = page.Locator(TotpInputFieldSelector);
            await otpInputField.WaitForAsync();
            await TotpLogin.PerformAsync(page, req.Profile.TotpSecret!, otpInputField);
        }
    }

    public async Task SignInFromLandingAsync(IPage page, UserProfile profile)
    {
        var locator = page.Locator(SignInSelector);
        await IsElementInteractable(locator);
        await locator.ClickAsync();

        await LoginAsync(page, new Contracts.LoginRequest(profile, string.IsNullOrWhiteSpace(profile.TotpSecret)
            ? Contracts.LoginMode.PasswordOnly
            : Contracts.LoginMode.Totp));
    }
}
