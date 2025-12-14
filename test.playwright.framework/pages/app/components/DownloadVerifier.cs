using FluentAssertions;
using Microsoft.Playwright;
using NUnit.Framework;

namespace test.playwright.framework.pages.app.components;

public static class DownloadVerifier
{
    public static async Task<string> WaitAndSaveAsync(this IPage page, Func<Task> triggerClick, string? saveDir = null,
        int timeoutMs = 15000, bool createUniqueSubDir = true)
    {
        var root = saveDir ?? Path.Combine(TestContext.CurrentContext.WorkDirectory, "Downloads");
        Directory.CreateDirectory(root);

        var targetDir = createUniqueSubDir && saveDir is null
            ? Path.Combine(root, Guid.NewGuid().ToString("N"))
            : root;

        Directory.CreateDirectory(targetDir);

        var wait = page.WaitForDownloadAsync(new PageWaitForDownloadOptions { Timeout = timeoutMs });
        await triggerClick();
        var dl = await wait;

        (await dl.FailureAsync()).Should().BeNull("download should complete without Playwright error");

        var suggested = dl.SuggestedFilename;
        if (string.IsNullOrWhiteSpace(suggested)) suggested = "download.bin";
        suggested = Path.GetFileName(suggested);

        var invalid = Path.GetInvalidFileNameChars();
        suggested = new string(suggested.Select(c => invalid.Contains(c) ? '_' : c).ToArray());

        var finalPath = Path.Combine(targetDir, suggested);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

        await dl.SaveAsAsync(finalPath);
        File.Exists(finalPath).Should().BeTrue($"downloaded file should exist: {finalPath}");
        new FileInfo(finalPath).Length.Should().BeGreaterThan(0, "downloaded file should not be empty");

        return finalPath;
    }

    public static void ExpectExtension(string path, string expectedExtWithoutDot)
    {
        var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        ext.Should().Be(expectedExtWithoutDot.ToLowerInvariant(), $"file extension should be .{expectedExtWithoutDot}");
    }

    public static void ExpectDeleted(string filePath)
    {
        try
        {
            if (File.Exists(filePath)) File.Delete(filePath);

            var dir = Path.GetDirectoryName(filePath)!;
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch (IOException)
        {
            Console.WriteLine("Failed to delete file.");
        }
    }
}