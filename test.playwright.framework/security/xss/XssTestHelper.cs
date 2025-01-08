using Microsoft.Playwright;
using Serilog;

namespace test.playwright.framework.security.xss;

public class XssTestHelper
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

    private static async Task InjectPayload(IPage page, string inputSelector, string payload,
        string searchButtonSelector)
    {
        await WaitForSelectorToExistAsync(page, inputSelector);
        var field = page.Locator(inputSelector);
        await field.FillAsync(payload);
        await WaitForSelectorToExistAsync(page, searchButtonSelector);
        var searchButton = page.Locator(searchButtonSelector);
        await searchButton.ClickAsync();
    }

    private static async Task<bool> ValidateSanitization(IPage page, string selector, string expectedValue)
    {
        var fieldValue = await page.Locator(selector).InputValueAsync();

        if (fieldValue == expectedValue)
        {
            Log.Information($"Field input is sanitized or retained as expected: {fieldValue}");
            return true;
        }

        Log.Error($"Sanitization failed. Expected: {expectedValue}, but got: {fieldValue}");
        return false;
    }

    private static async Task<bool> CheckForAlertDialog(IPage page)
    {
        var alertFired = false;

        page.Dialog += (_, dialog) =>
        {
            alertFired = true;
            dialog.DismissAsync().GetAwaiter().GetResult();
        };

        try
        {
            await Task.WhenAny(
                Task.Delay(2000),
                Task.Run(() =>
                {
                    while (!alertFired) Task.Delay(100).Wait();
                })
            );
        }
        catch (Exception ex)
        {
            Log.Error($"Error while waiting for alert dialog: {ex.Message}");
        }

        return alertFired;
    }

    public static async Task<XssTestReport> ValidateXssInjection(IPage page, string inputSelector,
        string searchButtonSelector)
    {
        var xssReport = new XssTestReport();
        var xssPayloads = XssPayloads.BasicPayloads
            .Concat(XssPayloads.ObfuscatedPayloads)
            .Concat(XssPayloads.EncodedPayloads);

        var enumerable = xssPayloads.ToList();
        foreach (var payload in enumerable)
        {
            Log.Information($"Testing payload: {payload}");
            await InjectPayload(page, inputSelector, payload, searchButtonSelector);

            var isSanitized = await ValidateSanitization(page, inputSelector, payload);
            var noAlertTriggered = !await CheckForAlertDialog(page);
            xssReport.TestDetails.Add(new XssTestDetails
            {
                FieldName = inputSelector,
                Payload = payload,
                IsSanitized = isSanitized,
                NoAlertTriggered = noAlertTriggered
            });

            if (isSanitized && noAlertTriggered)
            {
                xssReport.TotalPassed++;
            }
            else
            {
                xssReport.TotalFailed++;
                Log.Error($"XSS Test failed for payload: {payload}");
            }
        }

        xssReport.TotalPayloadsTested = enumerable.Count;
        xssReport.TotalFieldsTested++;
        return xssReport;
    }
}