namespace test.playwright.framework.pages.enums;

public enum ReportColumnKInd
{
    Customer,
    Title,
    Project,
    LanguageName,
    LanguageCode,
    TaskDescription,
    CompletedDate
}

internal static class CompletedTaskReportColumnKIndExt
{
    public static readonly IReadOnlyDictionary<ReportColumnKInd, string> Map =
        new Dictionary<ReportColumnKInd, string>
        {
            [ReportColumnKInd.Customer] = "Customer Name",
            [ReportColumnKInd.Title] = "Title",
            [ReportColumnKInd.Project] = "Project Name",
            [ReportColumnKInd.LanguageName] = "Language Name",
            [ReportColumnKInd.LanguageCode] = "Language Code",
            [ReportColumnKInd.TaskDescription] = "Task Description",
            [ReportColumnKInd.CompletedDate] = "Completed Date"
        };

    private static readonly IReadOnlyDictionary<string, ReportColumnKInd> UiToKind =
        Map.ToDictionary(p => p.Value, p => p.Key, StringComparer.OrdinalIgnoreCase);

    public static string ToUi(this ReportColumnKInd k) => Map.TryGetValue(k, out var ui)
        ? ui
        : throw new InvalidOperationException($"Report {k} has no UI label.");

    public static ReportColumnKInd ParseUi(this string uiText) =>
        UiToKind.TryGetValue(uiText, out var kind)
            ? kind
            : throw new ArgumentException($"Unknown report-row label “{uiText}”.");
}
