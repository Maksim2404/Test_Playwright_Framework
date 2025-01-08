using Microsoft.Playwright;
using Serilog;

namespace test.playwright.framework.security.sql;

public class SqlInjectionTestHelper
{
    private static async Task<bool> WaitForSelectorToExistAsync(IPage page, string selector, bool expectToExist = true)
    {
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded,
            new PageWaitForLoadStateOptions { Timeout = 10000 });

        try
        {
            var elementHandle = await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
            {
                State = expectToExist ? WaitForSelectorState.Attached : WaitForSelectorState.Detached,
                Timeout = expectToExist ? 20000 : 5000
            });

            if (elementHandle != null && expectToExist)
            {
                Log.Information($"Element with selector '{selector}' is visible on the page.");
                return true;
            }
            else if (elementHandle == null && !expectToExist)
            {
                Log.Information($"Confirmed absence of element with selector '{selector}'.");
                return true;
            }
        }
        catch (TimeoutException ex)
        {
            Log.Information(
                $"Element with selector '{selector}' not found or not visible within the specified timeout. Exception: {ex.Message}");
            return false;
        }

        return false;
    }

    private static async Task InjectPayloadAsync(IPage page, string inputSelector, string payload,
        string searchButtonSelector)
    {
        Log.Information($"Injecting payload: {payload}");
        await WaitForSelectorToExistAsync(page, inputSelector);
        var field = page.Locator(inputSelector);
        await field.FillAsync(payload);
        Log.Information($"Payload '{payload}' injected into selector: {inputSelector}");

        await WaitForSelectorToExistAsync(page, searchButtonSelector);
        var submitButton = page.Locator(searchButtonSelector);
        await submitButton.ClickAsync();
        Log.Information("Search button clicked.");
    }

    private static async Task<bool> DetectSqlErrorsAsync(IPage page)
    {
        var content = await page.ContentAsync();
        return content.Contains("syntax error") ||
               content.Contains("SQL") ||
               content.Contains("database error") ||
               content.Contains("unrecognized token");
    }

    public static async Task<SqlInjectionTestReport> ValidateSqlInjectionAsync(IPage page, string inputSelector,
        string searchButtonSelector)
    {
        var sqlInjectionReport = new SqlInjectionTestReport();
        var sqlPayloads = SqlInjectionPayloads.BasicPayloads.Concat(SqlInjectionPayloads.AdvancedPayloads).ToList();

        foreach (var payload in sqlPayloads)
        {
            Log.Information($"Testing SQL Injection payload: {payload}");
            await InjectPayloadAsync(page, inputSelector, payload, searchButtonSelector);

            var isSecure = !await DetectSqlErrorsAsync(page);
            sqlInjectionReport.TestDetails.Add(new SqlInjectionTestDetails
            {
                FieldName = inputSelector,
                Payload = payload,
                IsSanitized = isSecure,
                Notes = isSecure ? "Field is secure." : "Field is vulnerable to SQL Injection."
            });

            if (isSecure)
            {
                sqlInjectionReport.TotalPassed++;
            }
            else
            {
                sqlInjectionReport.TotalFailed++;
                Log.Error($"SQL Injection vulnerability detected for payload: {payload}");
            }
        }

        sqlInjectionReport.TotalPayloadsTested = sqlPayloads.Count;
        sqlInjectionReport.TotalFieldsTested++;
        return sqlInjectionReport;
    }
}