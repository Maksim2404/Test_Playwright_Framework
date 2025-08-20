using FluentAssertions;
using test.playwright.framework.pages.enums;

namespace test.playwright.framework.pages.reports;

public static class SpecificReportAssertions
{
    private const ReportColumnKInd Customer = ReportColumnKInd.Customer;
    private const ReportColumnKInd LanguageName = ReportColumnKInd.LanguageName;
    private const ReportColumnKInd LanguageCode = ReportColumnKInd.LanguageCode;
    private const ReportColumnKInd TaskDescription = ReportColumnKInd.TaskDescription;
    private const ReportColumnKInd Project = ReportColumnKInd.Project;
    private const ReportColumnKInd Title = ReportColumnKInd.Title;

    public static void ShouldContainRequiredColumns(string[] headers)
    {
        headers.Should().Contain([
            "Customer Name", "Title", "Project Name", "Language Name", "Language Code", "Task Description",
            "Completed Date"
        ], "Report must include key columns.");
    }

    public static void ShouldMatchPartsTaskRow(string[] headers, string[] row, ExpectedReportRow expected)
    {
        var cols = ReportTable.MapColumns(headers, Customer, LanguageName, LanguageCode,
            TaskDescription, Project, Title);

        // Required
        row[cols[Customer.ToUi()]].Should().Be(expected.CustomerName.ToUi());
        row[cols[Title.ToUi()]].Should().Be(expected.Title);
        row[cols[Project.ToUi()]].Should().Be(expected.ProjectName);
        row[cols[TaskDescription.ToUi()]].Should().Be(expected.TaskDescription);

        // Value semantics:
        if (cols.TryGetValue(LanguageName.ToUi(), out var ln))
            row[ln].Should().Be("Specific Value", "Specific Value must show 'Specific Value' in Language Name.");
        if (cols.TryGetValue(LanguageCode.ToUi(), out var lc))
            row[lc].Should().BeOneOf("", null, " ", "-", "—", "N/A", "None",
                "Language Code must be empty/none for 'Specific Value'.");

        // Optional - for example purposes only, bc Project is required value:
        if (!string.IsNullOrWhiteSpace(expected.ProjectName) && cols.TryGetValue(Project.ToUi(), out var pi))
            row[pi].Should().Be(expected.ProjectName);
    }

    public static void ShouldMatchLanguageTaskRow(string[] headers, string[] row, ExpectedReportRow expected)
    {
        var cols = ReportTable.MapColumns(headers, Customer, LanguageName, LanguageCode,
            TaskDescription, Project, Title);

        // Required
        row[cols[Customer.ToUi()]].Should().Be(expected.CustomerName.ToUi());
        row[cols[Title.ToUi()]].Should().Be(expected.Title);
        row[cols[Project.ToUi()]].Should().Be(expected.ProjectName);
        row[cols[TaskDescription.ToUi()]].Should().Be(expected.TaskDescription);

        cols.ContainsKey(LanguageName.ToUi()).Should().BeTrue("'Language Name' column missing.");
        cols.ContainsKey(LanguageCode.ToUi()).Should().BeTrue("'Language Code' column missing.");

        row[cols[LanguageName.ToUi()]].Should()
            .Be(expected.LanguageName ?? "", "Language tasks must include Language Name.");
        row[cols[LanguageCode.ToUi()]].Should()
            .Be(expected.LanguageCode ?? "", "Language tasks must include Language Code.");

        // Optional - for example purposes only, bc Project is required value:
        if (!string.IsNullOrWhiteSpace(expected.ProjectName) && cols.TryGetValue(Project.ToUi(), out var pi))
            row[pi].Should().Be(expected.ProjectName);
    }
}