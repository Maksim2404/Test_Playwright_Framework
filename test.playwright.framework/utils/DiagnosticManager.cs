using Allure.Net.Commons;
using Microsoft.Playwright;
using NUnit.Framework;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using test.playwright.framework.config;
using test.playwright.framework.utils.interfaces;

namespace test.playwright.framework.utils;

public class DiagnosticManager : IDiagnosticManager
{
    private readonly string? _screenshotPath;

    public DiagnosticManager(AtfConfig config)
    {
        _screenshotPath = config.ScreenshotPath;

        if (string.IsNullOrEmpty(_screenshotPath))
        {
            Log.Error("Screenshot path is not configured properly.");
            throw new InvalidOperationException("Screenshot path is missing.");
        }

        if (Directory.Exists(_screenshotPath)) return;
        Directory.CreateDirectory(_screenshotPath);
        Log.Information($"Created directory for screenshots: {_screenshotPath}");
    }

    public async Task<byte[]> CaptureScreenshotBufferAsync(IPage page)
    {
        try
        {
            var buffer = await page.ScreenshotAsync(new PageScreenshotOptions
            {
                FullPage = false
            });

            Log.Information("Screenshot captured as a buffer.");
            return buffer;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to capture screenshot buffer: {ex.Message}");
            return [];
        }
    }

    public async Task<string?> SaveBufferToFileAsync(byte[] buffer, string fileNamePrefix = "Screenshot",
        bool includeTimestamp = false)
    {
        try
        {
            if (string.IsNullOrEmpty(_screenshotPath))
            {
                Log.Error("Screenshot path is not configured properly.");
                return null;
            }

            var timestamp = includeTimestamp ? $"_{DateTime.Now:yyyyMMdd_HHmmss}" : string.Empty;
            var testName = TestContext.CurrentContext.Test.Name.Replace(" ", "_");
            var filePath = Path.Combine(_screenshotPath, $"{fileNamePrefix}_{testName}{timestamp}.png");

            await File.WriteAllBytesAsync(filePath, buffer);
            TestContext.AddTestAttachment(filePath, "Screenshot on error");
            Log.Information($"Screenshot saved to file: {filePath}");
            return filePath;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save screenshot buffer to file: {ex.Message}");
            return null;
        }
    }

    public void AttachBufferToReport(byte[] buffer)
    {
        try
        {
            AllureApi.AddAttachment("Screenshot", "image/png", buffer);
            Log.Information("Screenshot attached to Allure report.");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to attach screenshot buffer to Allure report: {ex.Message}");
        }
    }

    public async Task<string?> CaptureScreenshotAsync(IPage page, string fileNamePrefix = "Screenshot",
        bool includeTimestamp = false)
    {
        try
        {
            var timestamp = includeTimestamp ? $"_{DateTime.Now:yyyyMMdd_HHmmss}" : string.Empty;
            if (_screenshotPath != null)
            {
                var screenshotFilePath = Path.Combine(_screenshotPath, $"{fileNamePrefix}{timestamp}.png");

                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = screenshotFilePath,
                    FullPage = false
                });

                Log.Information($"Screenshot captured at: {screenshotFilePath}");
                TestContext.AddTestAttachment(screenshotFilePath, "Screenshot on error");
                return screenshotFilePath;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to capture screenshot: {ex.Message}");
        }

        return null;
    }

    public void CaptureVideoOfFailedTest(string videoDir, string failedVideoDir)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(videoDir);
            var myFile = directoryInfo.GetFiles().MaxBy(f => f.LastWriteTime);

            if (myFile == null) return;
            var sourceVideoPath = myFile.FullName;
            Directory.CreateDirectory(failedVideoDir);

            var targetVideoPath = Path.Combine(failedVideoDir,
                $"{TestContext.CurrentContext.Test.FullName}_{myFile.Name}");

            try
            {
                File.Copy(sourceVideoPath, targetVideoPath, true);
                TestContext.AddTestAttachment(targetVideoPath, "Video");
                Log.Information($"Successfully copied the video file: {sourceVideoPath} to {targetVideoPath}");
            }
            catch (IOException ex)
            {
                Log.Error(ex, $"Failed to copy the video file: {sourceVideoPath}. Error: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process the video file");
        }
    }

    public async Task<bool> CompareScreenshotsAsync(string baselinePath, string newPath, string diffPath)
    {
        try
        {
            if (!File.Exists(baselinePath))
            {
                Log.Error($"Baseline screenshot not found: {baselinePath}");
                return false;
            }

            if (!File.Exists(newPath))
            {
                Log.Error($"New screenshot not found: {newPath}");
                return false;
            }

            using var baselineImage = await Image.LoadAsync<Rgba32>(baselinePath);
            using var newImage = await Image.LoadAsync<Rgba32>(newPath);

            using var diffImage = new Image<Rgba32>(baselineImage.Width, baselineImage.Height);
            var areDifferent = false;

            for (var y = 0; y < baselineImage.Height; y++)
            {
                for (var x = 0; x < baselineImage.Width; x++)
                {
                    var pixelA = baselineImage[x, y];
                    var pixelB = newImage[x, y];

                    if (pixelA != pixelB)
                    {
                        areDifferent = true;
                        diffImage[x, y] = new Rgba32(255, 0, 0);
                    }
                    else
                    {
                        diffImage[x, y] = pixelA;
                    }
                }
            }

            if (!areDifferent) return !areDifferent;
            await diffImage.SaveAsync(diffPath);
            Log.Information($"Difference detected. Diff image saved at: {diffPath}");

            return !areDifferent;
        }
        catch (Exception ex)
        {
            Log.Error($"Error comparing images: {ex.Message}");
            return false;
        }
    }

    public static async Task CaptureTotpScreenshotAsync(IPage page, bool includeTimestamp = false)
    {
        try
        {
            var timestamp = includeTimestamp ? $"_{DateTime.Now:yyyyMMdd_HHmmss}" : string.Empty;
            const string screenshotDirectory = "totpFailedLoginScreenshots";
            Directory.CreateDirectory(screenshotDirectory);
            {
                var screenshotFilePath = Path.Combine(screenshotDirectory, $"totp_failed {timestamp}.png");

                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = screenshotFilePath,
                    FullPage = true
                });

                Log.Information($"Screenshot captured at: {screenshotFilePath}");
                TestContext.AddTestAttachment(screenshotFilePath, "Screenshot on error");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to capture screenshot: {ex.Message}");
        }
    }
}