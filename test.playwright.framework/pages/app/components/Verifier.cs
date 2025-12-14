using Serilog;

namespace test.playwright.framework.pages.app.components;

public static class Verifier
{
    public static bool LogEq(string field, string? actual, string? expected)
    {
        var ok = string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        Log.Information("[Entity] {Field} → expected: '{Expected}' | actual: '{Actual}' | ok={Ok}",
            field, expected ?? "«null»", actual ?? "«null»", ok);
        return ok;
    }

    public static bool LogSetEq(string field, IEnumerable<string> actual, IEnumerable<string> expected)
    {
        var act = actual.Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var exp = expected.Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var ok = act.SetEquals(exp);
        Log.Information("[Entity] {Field} set-equals -> expected: [{Exp}] | actual: [{Act}] | ok={Ok}",
            field, string.Join(", ", exp), string.Join(", ", act), ok);
        return ok;
    }

    public static bool LogContains(string field, string? actual, string needle)
    {
        var ok = !string.IsNullOrWhiteSpace(actual) && actual.Contains(needle, StringComparison.OrdinalIgnoreCase);
        Log.Information("[Entity] {Field} contains '{Needle}'? actual: '{Actual}' | ok={Ok}",
            field, needle, actual ?? "«null»", ok);
        return ok;
    }
}