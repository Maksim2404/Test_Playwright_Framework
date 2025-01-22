using Microsoft.Playwright;

namespace test.playwright.framework.utils.interfaces;

public interface IDiagnosticManager
{
    Task<byte[]> CaptureScreenshotBufferAsync(IPage page);

    Task<string?> SaveBufferToFileAsync(byte[] buffer, string fileNamePrefix = "Screenshot",
        bool includeTimestamp = false);

    void AttachBufferToReport(byte[] buffer);

    Task<string?> CaptureScreenshotAsync(IPage page, string fileNamePrefix = "Screenshot",
        bool includeTimestamp = false);

    void CaptureVideoOfFailedTest(string videoDir, string failedVideoDir);
}