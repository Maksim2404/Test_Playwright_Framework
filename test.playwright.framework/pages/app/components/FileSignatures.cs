using System.Text;
using FluentAssertions;

namespace test.playwright.framework.pages.app.components;

public static class FileSignatures
{
    public static async Task ExpectByExt(string path)
    {
        var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        switch (ext)
        {
            case "pdf": ExpectPdf(path); break;
            case "jpg":
            case "jpeg": ExpectJpeg(path); break;
            case "png": ExpectPng(path); break;
            case "gif": ExpectGif(path); break;
            case "webp": ExpectWebp(path); break;
            case "csv": await ExpectCsvAsync(path); break;
            case "txt": await ExpectTxtAsync(path); break;
            case "docx": ExpectDocx(path); break;
            case "xlsx": ExpectXlsx(path); break;
            case "zip": ExpectZip(path); break;

            default:
                var len = new FileInfo(path).Length;
                len.Should().BeGreaterThan(0, $"Unknown type '{ext}': file must not be empty");
                break;
        }
    }

    private static byte[] ReadHead(string path, int n = 16) => File.ReadAllBytes(path).Take(n).ToArray();

    private static void ExpectPdf(string path)
    {
        var h = ReadHead(path, 5);
        Encoding.ASCII.GetString(h).Should().Be("%PDF-", "PDF must start with %PDF-");
    }

    private static void ExpectJpeg(string path)
    {
        var h = ReadHead(path, 3);
        h[0].Should().Be(0xFF);
        h[1].Should().Be(0xD8);
        h[2].Should().Be(0xFF);
    }

    private static void ExpectPng(string path)
    {
        var h = ReadHead(path, 8);
        var sig = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        h.Should().StartWith(sig, "PNG signature mismatch");
    }

    private static void ExpectGif(string path)
    {
        var s = Encoding.ASCII.GetString(ReadHead(path, 6));
        (s is "GIF87a" or "GIF89a").Should().BeTrue("GIF header must be GIF87a or GIF89a");
    }

    private static void ExpectWebp(string path)
    {
        var h = ReadHead(path, 12);
        Encoding.ASCII.GetString(h, 0, 4).Should().Be("RIFF");
        Encoding.ASCII.GetString(h, 8, 4).Should().Be("WEBP");
    }

    private static void ExpectZip(string path)
    {
        var h = ReadHead(path, 4);
        var ok = (h[0] == 0x50 && h[1] == 0x4B && (h[2] == 0x03 || h[2] == 0x05) && (h[3] == 0x04 || h[3] == 0x06));
        ok.Should().BeTrue(@"ZIP must start with PK\x03\x04 or PK\x05\x06");
    }

    private static void ExpectDocx(string path)
    {
        ExpectZip(path);
        using var zip = System.IO.Compression.ZipFile.OpenRead(path);
        zip.Entries.Any(e => e.FullName.Equals("[Content_Types].xml", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue("DOCX must contain [Content_Types].xml");
        zip.Entries.Any(e => e.FullName.StartsWith("word/", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue("DOCX must contain 'word/' parts");
    }

    private static void ExpectXlsx(string path)
    {
        ExpectZip(path);
        using var zip = System.IO.Compression.ZipFile.OpenRead(path);
        zip.Entries.Any(e => e.FullName.Equals("[Content_Types].xml", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue("XLSX must contain [Content_Types].xml");
        zip.Entries.Any(e => e.FullName.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue("XLSX must contain 'xl/' parts");
    }

    private static async Task<(bool IsText, string Sample)> ProbeTextAsync(string path, int maxBytes = 4096)
    {
        byte[] buf;
        await using (var fs = File.OpenRead(path))
        {
            buf = new byte[Math.Min(maxBytes, (int)fs.Length)];
            _ = await fs.ReadAsync(buf);
        }

        if (buf.Any(b => b == 0x00)) return (false, "");

        var txt = Encoding.UTF8.GetString(buf);
        var printable = txt.Count(c => !char.IsControl(c) || c is '\r' or '\n' or '\t');
        var ratio = (double)printable / Math.Max(1, txt.Length);
        return (ratio >= 0.98, txt);
    }

    private static async Task ExpectCsvAsync(string path)
    {
        var (isText, sample) = await ProbeTextAsync(path);
        isText.Should().BeTrue("CSV should be plain text (UTF-8 preferred)");
        sample.IndexOf('\n').Should().BeGreaterThan(0, "CSV should contain newlines");

        (sample.Contains(',') || sample.Contains(';') || sample.Contains('\t'))
            .Should().BeTrue("CSV should contain a common delimiter (',' ';' or TAB)");
    }

    private static async Task ExpectTxtAsync(string path)
    {
        var (isText, _) = await ProbeTextAsync(path);
        isText.Should().BeTrue("TXT should be plain text (UTF-8 preferred)");
    }
}