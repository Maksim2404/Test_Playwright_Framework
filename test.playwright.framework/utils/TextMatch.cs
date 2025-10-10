using System.Text.RegularExpressions;
using NUnit.Framework;

namespace test.playwright.framework.utils;

public static partial class TextMatch
{
    [GeneratedRegex(@"\s+")]
    private static partial Regex MyRegex();

    public static string Normalize(string? s, bool collapseWhitespace = true)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.Trim();
        return collapseWhitespace
            ? MyRegex().Replace(s, " ")
            : s;
    }

    public static bool Equals(string? actual, string? expected, bool ignoreCase = false, bool collapseWhitespace = true)
    {
        var a = Normalize(actual, collapseWhitespace);
        var e = Normalize(expected, collapseWhitespace);
        var cmp = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(a, e, cmp);
    }

    public static bool Contains(string? actual, string? expected, bool ignoreCase = false,
        bool collapseWhitespace = true)
    {
        var a = Normalize(actual, collapseWhitespace);
        var e = Normalize(expected, collapseWhitespace);
        return a.Contains(e, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}

[TestFixture]
public class TextMatchTests
{
    [TestCase("Hello", "Hello", false, true, true)]
    [TestCase("Hello", "hello", true, true, true)]
    [TestCase("Hello", "hello", false, true, false)]
    [TestCase("  Hello  ", "Hello", false, true, true)]
    [TestCase("A   B", "A B", false, true, true)]
    [TestCase("A   B", "A   B", false, false, true)]
    [TestCase("", "", false, true, true)]
    [TestCase(null, "", false, true, true)]
    public void Equals_Works(string? actual, string? expected, bool ignoreCase, bool collapseWs, bool expectedResult)
    {
        var ok = TextMatch.Equals(actual, expected, ignoreCase, collapseWs);
        Assert.That(ok, Is.EqualTo(expectedResult));
    }

    [TestCase("Hello world", "world", false, true, true)]
    [TestCase("Hello world", "WORLD", true, true, true)]
    [TestCase("Hello world", "WORLD", false, true, false)]
    [TestCase("A   B   C", "B C", false, true, true)]
    [TestCase("A\nB\tC", "B C", false, true, true)]
    [TestCase("X", "XYZ", false, true, false)]
    [TestCase(null, "X", false, true, false)]
    [TestCase("   ", "", false, true, true)]
    public void Contains_Works(string? actual, string? needle, bool ignoreCase, bool collapseWs, bool expectedResult)
    {
        var ok = TextMatch.Contains(actual, needle, ignoreCase, collapseWs);
        Assert.That(ok, Is.EqualTo(expectedResult));
    }

    [Test]
    public void Normalize_CollapsesWhitespace()
    {
        var n = TextMatch.Normalize("  A   \n  B\tC  ");
        Assert.That(n, Is.EqualTo("A B C"));
    }

    [Test]
    public void Normalize_RespectsCollapseFlag()
    {
        var n = TextMatch.Normalize("  A   \n  B\tC  ", collapseWhitespace: false);
        Assert.That(n, Is.EqualTo("A   \n  B\tC"));
    }
}