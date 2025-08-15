using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using test.playwright.framework.utils.interfaces;

namespace test.playwright.framework.utils.diagnostics;

public sealed class PixelImageComparer : IImageComparer
{
    public async Task<bool> AreEqualAsync(string baseline, string actual, string diffPath)
    {
        using var imgA = await Image.LoadAsync<Rgba32>(baseline);
        using var imgB = await Image.LoadAsync<Rgba32>(actual);

        if (imgA.Width != imgB.Width || imgA.Height != imgB.Height) return false;

        var diff = new Image<Rgba32>(imgA.Width, imgA.Height);
        var diffFound = false;

        for (var y = 0; y < imgA.Height; y++)
        for (var x = 0; x < imgA.Width; x++)
        {
            var pa = imgA[x, y];
            var pb = imgB[x, y];
            if (pa == pb) continue;
            diff[x, y] = new Rgba32(255, 0, 0);
            diffFound = true;
        }

        if (diffFound) await diff.SaveAsync(diffPath);
        return !diffFound;
    }
}
