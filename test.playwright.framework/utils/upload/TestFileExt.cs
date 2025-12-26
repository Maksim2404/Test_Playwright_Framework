using test.playwright.framework.fixtures.constants;

namespace test.playwright.framework.utils.upload;

public enum UploadArea
{
    ProfileDocuments
}

internal static class TestFileExt
{
    private static readonly IReadOnlyDictionary<TestFile, (string FileName, UploadArea Area)> Map =
        new Dictionary<TestFile, (string FileName, UploadArea Area)>
        {
            [TestFile.Pic] = ("testImage.jpg", UploadArea.ProfileDocuments),
            [TestFile.Pdf] = ("testPDF.pdf", UploadArea.ProfileDocuments),
            [TestFile.Txt] = ("testTxt.txt", UploadArea.ProfileDocuments),
            [TestFile.Csv] = ("testCsv.csv", UploadArea.ProfileDocuments)
        };

    public static string FileName(this TestFile f) => Map[f].FileName;
    public static UploadArea Area(this TestFile f) => Map[f].Area;

    public static string AbsolutePath(this TestFile f) =>
        Path.Combine(Directory.GetCurrentDirectory(), TestDataConstants.FilesToUploadDirectory, f.FileName());

    public static string ResolveAbsolutePath(string fileName)
    {
        var p = Path.Combine(Directory.GetCurrentDirectory(), TestDataConstants.FilesToUploadDirectory, fileName);
        if (!File.Exists(p))
            throw new FileNotFoundException($"Test file not found: {p}");
        return p;
    }
}