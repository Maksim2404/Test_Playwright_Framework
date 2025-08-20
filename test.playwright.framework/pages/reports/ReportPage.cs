using System.Globalization;
using FluentAssertions;
using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.base_abstract;
using test.playwright.framework.utils;

namespace test.playwright.framework.pages.reports;

public class ReportPage(IPage page) : BaseProjectElements(page)
{
    private const string UiDateFormatLiteralSlash = "MM'/'dd'/'yyyy";
    private ILocator DownloadReportButton => Page.Locator("//button[@data-test='downloadReportsButton']");
    private ILocator StartDateSearchField => Page.Locator("//div[@data-test='startDateFilter']//input");
    private ILocator EndDateSearchField => Page.Locator("//div[@data-test='endDateFilter']//input");
    private ILocator NoDataBanner => Page.Locator("div.v-overlay__content h1:has-text('The report has no data.')");
    private ILocator StartDateAlert => Page.Locator("//startDateAlert");
    private ILocator EndDateAlert => Page.Locator("//endDateAlert");

    private ILocator CustomerErrorChip =>
        Page.Locator("//div[contains(@class, 'v-input--error') and @data-test='customerSearchField']");

    private ILocator QuickFilterChip(string label) =>
        Page.Locator($"//span[@data-test='quickFilterButton']//div[@class='v-chip__content'][text()='{label}']");

    public async Task<ReportPage> ClickDownloadReportButton()
    {
        await Click(DownloadReportButton);
        return this;
    }

    public async Task<ReportPage> ClickQuickFilterThisMonth()
    {
        await Click(QuickFilterChip("This Month"));
        return this;
    }

    public async Task<ReportPage> ClickQuickFilterMonthBack(int monthsBack, DateTimeOffset? now = null)
    {
        now ??= DateTimeOffset.Now;
        var label = ReportDateRanges.MonthChipLabelForNBack(now.Value, monthsBack);
        await Click(QuickFilterChip(label));
        return this;
    }

    public async Task<ReportPage> SetDateRange(DateTimeOffset start, DateTimeOffset end)
    {
        var startDate = start.ToString(UiDateFormatLiteralSlash, CultureInfo.InvariantCulture);
        var endDate = end.ToString(UiDateFormatLiteralSlash, CultureInfo.InvariantCulture);

        await Input(StartDateSearchField, startDate);
        await Input(EndDateSearchField, endDate);

        var (startUi, endUi) = await ReadDateRangeAsync();
        startUi.Should().Be(startDate);
        endUi.Should().Be(endDate);
        return this;
    }

    public async Task<(string Start, string End)> ReadDateRangeAsync()
    {
        var start = await StartDateSearchField.InputValueAsync();
        var end = await EndDateSearchField.InputValueAsync();
        return (start, end);
    }

    public async Task<bool> IsCustomerInvalid()
    {
        var present = await CustomerErrorChip.IsVisibleWithinAsync();
        if (present)
            Log.Information("Customer field is empty -> validation is correct.");
        else
            Log.Error("Validation missed! However, the customer field is required in order to run report.");

        return present;
    }

    public async Task<bool> VerifyErrorWhenReportEmpty()
    {
        var isAlertPresent = await WaitAndVerifySingleElementText("The report has no data.", NoDataBanner);
        if (isAlertPresent)
            Log.Information("Validation found and verified successfully after generating an empty report.");
        else
            Log.Error("Validation wasn't displayed after generating an empty report.");

        return isAlertPresent;
    }

    // The success signal for a report is "download happened"
    public async Task<IDownload> RunAndWaitForDownloadAsync(Func<Task>? beforeRun = null)
    {
        if (beforeRun is not null) await beforeRun();
        var download = await Page.RunAndWaitForDownloadAsync(async () => { await ClickDownloadReportButton(); });
        return download;
    }

    public async Task ClickRunAsync() => await ClickDownloadReportButton();

    public async Task<bool> IsStartDateInvalidAsync() => await StartDateAlert.IsVisibleWithinAsync();
    public async Task<bool> IsEndDateInvalidAsync() => await EndDateAlert.IsVisibleWithinAsync();

    public async Task<string?> GetStartDateErrorAsync()
    {
        try
        {
            await StartDateAlert.IsVisibleWithinAsync(2000);
            var txt = (await StartDateAlert.InnerTextAsync()).Trim();
            return string.IsNullOrWhiteSpace(txt) ? null : txt;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while trying to get start date error.");
            return null;
        }
    }

    public async Task<string?> GetEndDateErrorAsync()
    {
        try
        {
            await EndDateAlert.IsVisibleWithinAsync(2000);
            var txt = (await EndDateAlert.InnerTextAsync()).Trim();
            return string.IsNullOrWhiteSpace(txt) ? null : txt;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while trying to get end date error.");
            return null;
        }
    }
}
