namespace test.playwright.framework.utils.upload;

public sealed record TestFileInfo(string FileName, string ContentType);

public static class TestFiles
{
    private static readonly IReadOnlyDictionary<TestFile, TestFileInfo> Map =
        new Dictionary<TestFile, TestFileInfo>
        {
            [TestFile.Pic] = new("testImage.jpg", "image/jpeg"),
            [TestFile.Pdf] = new("testPDF.pdf", "application/pdf"),
            [TestFile.Txt] = new("testTxt.txt", "text/plain"),
            [TestFile.Csv] = new("testCsv.csv", "text/csv")

            // Possible future extensions:
            // [TestFile.docx] = new("testDocx.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
            // [TestFile.xlsx] = new("testXlsx.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        };

    public static TestFileInfo Info(this TestFile f) => Map[f];
    public static TestFile PickDefaultDoc() => TestFile.Pdf;

    public static TestFile PickRandom(Func<TestFile, bool>? filter = null)
    {
        var pool = Enum.GetValues<TestFile>().AsEnumerable();
        if (filter is not null) pool = pool.Where(filter);

        var arr = pool.ToArray();
        if (arr.Length == 0) throw new InvalidOperationException("No TestFiles available for the given filter.");

        return arr[Random.Shared.Next(arr.Length)];
    }

    public static TestFile PickRandomNonImage() => PickRandom(f => f != TestFile.Pic);
}