using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.base_abstract;
using test.playwright.framework.pages.enums;
using test.playwright.framework.utils;

namespace test.playwright.framework.pages.mail;

public sealed record MailSpec(string Subject, string BodyCss, string? ExpectedBody);

public class MailPage(IPage page, IPage original) : BaseProjectElements(page)
{
    private ILocator IframeLocator => Page.Locator("iframe#preview-html");
    private ILocator SubjectLine(string subject) => Page.Locator($"//div/b[contains(text(), '{subject}')]");

    public async Task CloseAndReturnAsync()
    {
        await Page.CloseAsync();
        await original.BringToFrontAsync();
    }

    public async Task ClickEmailAsync(MailSpec spec)
    {
        await ClickAsync(SubjectLine(spec.Subject));
    }

    private async Task<IFrameLocator> EmailBodyAsync()
    {
        await IframeLocator.ExpectSingleVisibleAsync();
        return IframeLocator.ContentFrame ?? throw new Exception("Failed to get iframe content.");
    }

    public async Task<bool> VerifyBodyAsync(MailSpec spec)
    {
        if (spec.ExpectedBody is null)
            throw new InvalidOperationException(
                "ExpectedBody is null—build the MailSpec with a user for verification.");

        var iframe = await EmailBodyAsync();
        var locator = iframe.Locator(spec.BodyCss);

        var expected = spec.ExpectedBody;
        return await WaitAndVerifySingleElementText(expected, locator);
    }

    private async Task<ILocator> WaitForSubjectAsync(MailSpec spec, int timeoutMs = 15000)
    {
        var loc = SubjectLine(spec.Subject);
        await loc.ExpectSingleVisibleAsync(timeoutMs);
        Log.Information("Email subject appeared: '{Subject}'", spec.Subject);
        return loc;
    }

    public async Task VerifySubjectContainsAsync(MailSpec spec, params string[] parts)
    {
        var loc = await WaitForSubjectAsync(spec);
        var actual = (await loc.InnerTextAsync()).Trim();
        Log.Information("Asserting subject contains tokens {@Parts}. Actual: '{Actual}'", parts, actual);

        foreach (var p in parts)
        {
            try
            {
                await Assertions.Expect(loc).ToContainTextAsync(p);
            }
            catch (PlaywrightException ex)
            {
                Log.Error(ex, "Subject missing token '{Part}'. Actual: '{Actual}'", p, actual);
                throw;
            }
        }
    }

    public async Task VerifySubjectEqualsAsync(MailSpec spec)
    {
        var loc = await WaitForSubjectAsync(spec);
        var actual = (await loc.InnerTextAsync()).Trim();
        Log.Information("Asserting subject equals. Expected: '{Expected}', Actual: '{Actual}'", spec.Subject, actual);
        await Assertions.Expect(loc).ToHaveTextAsync(spec.Subject);
    }

    public async Task<bool> VerifyEmailDetails(CustomerKind customer)
    {
        var iframe = await EmailBodyAsync();

        var expectedDetails = new Dictionary<string, object>
        {
            { "Customer", customer.ToUi() },
        };

        return await VerifyTableDetailsAsync(iframe, expectedDetails, customer.ToUi());
    }
}