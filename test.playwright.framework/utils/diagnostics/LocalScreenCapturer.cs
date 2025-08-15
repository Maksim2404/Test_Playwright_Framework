using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.utils.interfaces;

namespace test.playwright.framework.utils.diagnostics;

public sealed class LocalScreenCapturer : IScreenCapturer
{
    public async Task<byte[]> CaptureAsync(IPage page, bool fullPage = false)
    {
        if (page.IsClosed)
        {
            Log.Warning("Page already closed – skipping screenshot.");
            return [];
        }

        try
        {
            return await page.ScreenshotAsync(new PageScreenshotOptions { FullPage = fullPage });
        }
        catch (PlaywrightException)
        {
            Log.Warning("Playwright closed the target while capturing screenshot – skipping.");
            return [];
        }
    }
}
