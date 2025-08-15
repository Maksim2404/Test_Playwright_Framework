using NUnit.Framework;
using Serilog;
using test.playwright.framework.utils.interfaces;

namespace test.playwright.framework.utils.diagnostics;

public sealed class VideoCopier(ILogger log) : IVideoCopier
{
    public void CopyLastVideo(string sourceDir, string destDir, string testName)
    {
        var last = new DirectoryInfo(sourceDir)
            .GetFiles()
            .OrderByDescending(f => f.LastWriteTime)
            .FirstOrDefault();

        if (last is null) return;

        Directory.CreateDirectory(destDir);
        var target = Path.Combine(destDir, $"{testName}_{last.Name}");
        File.Copy(last.FullName, target, true);
        TestContext.AddTestAttachment(target, "Video");
        log.Information("Copied video to {Target}", target);
    }
}
