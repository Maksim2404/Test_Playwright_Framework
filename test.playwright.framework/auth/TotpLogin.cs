using Microsoft.Playwright;
using OtpNet;
using Serilog;
using test.playwright.framework.utils;

namespace test.playwright.framework.auth;

public static class TotpLogin
{
    public static async Task PerformAsync(IPage page, string? totpSecret, ILocator otpInputLocator)
    {
        if (string.IsNullOrWhiteSpace(totpSecret))
            throw new InvalidOperationException(
                "User profile says 2-FA is required but TotpSecret is empty / missing.");

        async Task<bool> TryEnterCodeAsync()
        {
            var code = GenerateTotpCode(totpSecret, out var secondsLeft);

            Log.Information("Generated TOTP {Code} (expires in {Seconds}s)", code, secondsLeft);

            await otpInputLocator.FillAsync(code);
            await otpInputLocator.PressAsync("Enter");

            return !await otpInputLocator.IsVisibleAsync();
        }

        if (await TryEnterCodeAsync())
            return;

        // optional retry with next time-window
        Log.Warning("TOTP failed on first attempt – retrying with next time window…");
        await page.WaitForTimeoutAsync(31_000);

        if (await TryEnterCodeAsync())
            return;

        Log.Error("TOTP login failed twice – taking screenshot.");
        await DiagnosticManager.CaptureTotpScreenshotAsync(page);
        throw new Exception("TOTP login failed (code rejected twice).");
    }

    private static string GenerateTotpCode(string secret, out int secondsRemaining)
    {
        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes);
        secondsRemaining = totp.RemainingSeconds();
        var code = totp.ComputeTotp(); // defaults: 30 s, SHA-1, 6 digits
        return code;
    }
}
