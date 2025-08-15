using Allure.Net.Commons;
using NUnit.Framework;
using Serilog;
using test.playwright.framework.utils.interfaces;

namespace test.playwright.framework.utils.diagnostics;

public sealed class FileSystemArtifactStore : IArtifactStore
{
    private readonly string _root;
    private readonly ILogger _log;

    public FileSystemArtifactStore(string root, ILogger log)
    {
        _root = string.IsNullOrWhiteSpace(root) ? throw new ArgumentNullException(nameof(root)) : root;
        _log = log;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveScreenshotAsync(byte[] buffer, string name = "Screenshot")
    {
        var file = Path.Combine(_root, $"{name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");
        await File.WriteAllBytesAsync(file, buffer);
        TestContext.AddTestAttachment(file);
        _log.Information("Saved screenshot to {Path}", file);
        return file;
    }

    public void AttachToReport(byte[] buffer, string title) => AllureApi.AddAttachment(title, "image/png", buffer);
}
