using Microsoft.Playwright;

namespace test.playwright.framework.utils.interfaces;

public interface IScreenCapturer
{
    Task<byte[]> CaptureAsync(IPage page, bool fullPage = false);
}
