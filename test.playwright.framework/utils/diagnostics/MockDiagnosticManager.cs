using Microsoft.Playwright;
using Moq;
using NUnit.Framework;
using test.playwright.framework.utils.interfaces;

namespace test.playwright.framework.utils.diagnostics;

internal sealed class StubCapturer : IScreenCapturer
{
    public List<byte[]> Buffers { get; } = [];

    public Task<byte[]> CaptureAsync(IPage _, bool __ = false)
    {
        var png = new byte[] { 9, 9, 9 };
        Buffers.Add(png);
        return Task.FromResult(png);
    }
}

internal sealed class StubArtifactStore : IArtifactStore
{
    public List<string> SavedFiles { get; } = [];

    public void AttachToReport(byte[] _, string __)
    {
        /* no‑op */
    }

    public Task<string> SaveScreenshotAsync(byte[] _, string name)
    {
        var path = $"{name}.png";
        SavedFiles.Add(path);
        return Task.FromResult(path);
    }
}

[TestFixture]
public class ScreenshotHelpersTests
{
    [Test]
    public async Task CaptureAsync_ReturnsBuffer()
    {
        var capturer = new StubCapturer();
        var page = new Mock<IPage>();

        var png = await capturer.CaptureAsync(page.Object);

        Assert.That(png, Is.Not.Empty);
        Assert.That(capturer.Buffers, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ArtifactStore_SavesFile()
    {
        var store = new StubArtifactStore();
        var png = new byte[] { 1, 2, 3 };

        var path = await store.SaveScreenshotAsync(png, "test");

        Assert.That(path, Does.EndWith(".png"));
        Assert.That(store.SavedFiles, Contains.Item(path));
    }
}
