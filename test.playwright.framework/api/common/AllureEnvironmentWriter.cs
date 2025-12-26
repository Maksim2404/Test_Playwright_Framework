using System.Text;

namespace test.playwright.framework.api.common;

public static class AllureEnvironmentWriter
{
    public static void Write(string resultsDir, params (string Key, string Value)[] pairs)
    {
        Directory.CreateDirectory(resultsDir);

        var path = Path.Combine(resultsDir, "environment.properties");
        var sb = new StringBuilder();

        foreach (var (k, v) in pairs) sb.AppendLine($"{k}={v}");

        File.WriteAllText(path, sb.ToString());
    }

    public static string Safe(string name)
    {
        name = Path.GetInvalidFileNameChars().Aggregate(name, (current, c) => current.Replace(c, '_'));
        name = name.Replace("/", "_").Replace("\\", "_").Replace(":", "_");
        return name;
    }
}