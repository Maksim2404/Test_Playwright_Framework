namespace test.playwright.framework.utils;

public static class NameParser
{
    public readonly record struct NameParts(string First, string? Last);

    public static NameParts Parse(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return new NameParts("", null);

        var tokens = fullName.Trim()
            .Split([' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().Trim(',', '.'))
            .ToList();

        if (tokens.Count > 1) tokens.RemoveAt(tokens.Count - 1);
        if (tokens.Count == 1) return new NameParts(tokens[0], null);

        var first = tokens[0];
        var last = tokens[^1];
        return new NameParts(first, last);
    }
}