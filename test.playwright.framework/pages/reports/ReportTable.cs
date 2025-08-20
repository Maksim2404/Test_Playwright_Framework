using FluentAssertions;
using test.playwright.framework.pages.enums;

namespace test.playwright.framework.pages.reports;

public static class ReportTable
{
    public static int ColIndex(string[] headers, string name) =>
        Array.FindIndex(headers, h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase));

    public static Dictionary<string, int> MapColumns(string[] headers, params ReportColumnKInd[] names)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in names)
        {
            var i = ColIndex(headers, n.ToUi());
            if (i >= 0) map[n.ToUi()] = i;
        }

        return map;
    }

    public static string[] FindRow(string[] headers, List<string[]> rows, ExpectedReportRow expected)
    {
        const ReportColumnKInd customer = ReportColumnKInd.Customer;
        const ReportColumnKInd taskDescription = ReportColumnKInd.TaskDescription;
        const ReportColumnKInd project = ReportColumnKInd.Project;
        const StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;

        var cols = MapColumns(headers, customer, taskDescription, project);
        cols.ContainsKey(customer.ToUi()).Should().BeTrue("'Customer Name' column missing.");
        cols.ContainsKey(taskDescription.ToUi()).Should().BeTrue("'Task Description' column missing.");
        cols.ContainsKey(project.ToUi()).Should().BeTrue("'Project Name' column missing.");

        var matches = rows.Where(r =>
            string.Equals(r[cols[customer.ToUi()]], expected.CustomerName.ToUi(), ignoreCase) &&
            string.Equals(r[cols[taskDescription.ToUi()]], expected.TaskDescription, ignoreCase) &&
            string.Equals(r[cols[project.ToUi()]], expected.ProjectName, ignoreCase)
        ).ToList();

        matches.Count.Should().BeGreaterThan(0, "Expected report to include the completed task row.");
        matches.Count.Should().BeLessThan(2, "Expected a single matching row, but found duplicates.");
        return matches[0];
    }
}
