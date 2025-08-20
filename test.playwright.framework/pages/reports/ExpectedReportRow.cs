using test.playwright.framework.pages.enums;

namespace test.playwright.framework.pages.reports;

public sealed record ExpectedReportRow(
    CustomerKind CustomerName,
    string Title,
    string ProjectName,
    string PartNumber,
    string TaskDescription,
    string? LanguageName = null,
    string? LanguageCode = null
);
