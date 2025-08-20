using FluentAssertions;
using Microsoft.Playwright;

namespace test.playwright.framework.pages.reports;

public static class ReportAssertions
{
    public static async Task ShouldNotDownload(this ReportPage page, Func<Task> run)
    {
        var downloaded = false;
        page.Page.Download += Handler;
        try
        {
            await run();
            downloaded.Should().BeFalse("No download should occur.");
        }
        finally
        {
            page.Page.Download -= Handler;
        }

        return;

        void Handler(object? s, IDownload d) => downloaded = true;
    }

    public static async Task<(string[] Headers, List<string[]> Rows)> ShouldDownloadXlsx(
        this ReportPage page)
    {
        var dl = await page.RunAndWaitForDownloadAsync();

        var name = dl.SuggestedFilename;
        name.Should().MatchRegex(@"^Completed_tasks_\d{4}[-_]\d{2}[-_]\d{2}( \(\d+\))?\.xlsx$");

        await using var src = await dl.CreateReadStreamAsync();
        src.Should().NotBeNull();

        using var ms = new MemoryStream();
        await src.CopyToAsync(ms);
        ms.Position = 0;

        var expectedHeaders = new[]
        {
            "Customer Name", "Title", "Project Name", "Language Name", "Language Code", "Task Description",
            "Completed Date"
        };

        var (headers, rows) = Xlsx.ReadFirstSheetAutoHeader(ms, expectedHeaders);
        headers.Should().NotBeEmpty("XLSX must include header row.");
        return (headers, rows);
    }

    public static async Task ShouldShowDateErrorsAndNotDownload(this ReportPage page,
        string? expectedStartError = null, string? expectedEndError = null)
    {
        await page.ShouldNotDownload(page.ClickRunAsync);

        if (expectedStartError is not null)
        {
            (await page.IsStartDateInvalidAsync()).Should().BeTrue("Start date should be marked invalid.");
            (await page.GetStartDateErrorAsync()).Should().Be(expectedStartError);
        }

        if (expectedEndError is not null)
        {
            (await page.IsEndDateInvalidAsync()).Should().BeTrue("End date should be marked invalid.");
            (await page.GetEndDateErrorAsync()).Should().Be(expectedEndError);
        }
    }
}