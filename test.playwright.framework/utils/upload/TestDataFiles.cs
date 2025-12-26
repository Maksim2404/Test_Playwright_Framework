namespace test.playwright.framework.utils.upload;

public static class TestDataFiles
{
    public const string FilesDir = "TestFilesToUpload";

    public static string AbsolutePath(string fileName)
    {
        var baseDir = AppContext.BaseDirectory;
        var path = Path.Combine(baseDir, FilesDir, fileName);

        if (!File.Exists(path)) throw new FileNotFoundException($"Test file not found: {path}");

        return path;
    }

    public static byte[] ReadAllBytes(string fileName) => File.ReadAllBytes(AbsolutePath(fileName));

    public static (byte[] Bytes, string FileName, string ContentType) Load(TestFile f)
    {
        var info = f.Info();
        var bytes = ReadAllBytes(info.FileName);
        return (bytes, info.FileName, info.ContentType);
    }
}