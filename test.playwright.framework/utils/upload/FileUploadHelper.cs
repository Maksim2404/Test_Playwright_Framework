using Microsoft.Playwright;
using Serilog;

namespace test.playwright.framework.utils.upload;

public static class FileUploadHelper
{
    private const string FileInputSelector = "//uploadLocator";

    public static async Task<List<string>> UploadFilesAsync(IPage page, UploadArea area, int count,
        Func<TestFile, bool>? filter = null)
    {
        var pool = Enum.GetValues<TestFile>().Where(f => f.Area() == area);
        if (filter is not null) pool = pool.Where(filter);

        var selected = pool.OrderBy(_ => Guid.NewGuid()).Take(count).ToArray();
        if (selected.Length < count)
            throw new InvalidOperationException(
                $"Requested {count} files, only {selected.Length} available for {area}.");

        var paths = selected.Select(tf => tf.AbsolutePath()).ToArray();
        Log.Information("Uploading {Cnt} file(s) to {Area}: {Files}",
            selected.Length, area, string.Join(", ", selected.Select(f => f.FileName())));

        await page.Locator(FileInputSelector).SetInputFilesAsync(paths);
        return selected.Select(f => f.FileName()).ToList();
    }

    public static Task<List<string>> UploadProfileDocuments(IPage page, int count) =>
        UploadFilesAsync(page, UploadArea.ProfileDocuments, count);

    // Explicit by enum(s)
    public static async Task<List<string>> UploadFilesAsync(IPage page, params TestFile[] files)
    {
        if (files.Length == 0) throw new ArgumentException("No files specified.");
        var area = files[0].Area();
        if (files.Any(f => f.Area() != area))
            throw new ArgumentException("All files must target the same UploadArea.");

        var paths = files.Select(f => f.AbsolutePath()).ToArray();
        await page.Locator(FileInputSelector).SetInputFilesAsync(paths);
        return files.Select(f => f.FileName()).ToList();
    }

    // Explicit by filename(s)
    public static async Task<List<string>> UploadFilesByNameAsync(IPage page, IEnumerable<string> fileNames)
    {
        var names = fileNames.ToList();
        if (names.Count == 0) throw new ArgumentException("No file names specified.");

        var paths = names.Select(TestFileExt.ResolveAbsolutePath).ToArray();
        await page.Locator(FileInputSelector).SetInputFilesAsync(paths);
        return names;
    }
}