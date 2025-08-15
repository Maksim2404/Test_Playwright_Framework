using Microsoft.Playwright;

namespace test.playwright.framework.utils;

public static class LocatorFactory
{
    /// <summary>
    /// Ensures that exactly one element matches <paramref name="locator"/>
    /// and that it is visible within <paramref name="timeoutMs"/>.
    /// Throws – like the built-in assertions – when the expectation is not met.
    /// </summary>
    public static async Task ExpectSingleVisibleAsync(this ILocator locator, int timeoutMs = 10_000)
    {
        await Assertions.Expect(locator)
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeoutMs });

        await Assertions.Expect(locator)
            .ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = timeoutMs });
    }

    /// <summary>
    /// Fails unless the locator shows <paramref name="min"/> visible elements
    /// within <paramref name="timeoutMs"/>.
    /// </summary>
    public static async Task ExpectVisibleCountAsync(this ILocator locator, int min = 1, int timeoutMs = 10_000)
    {
        await Assertions.Expect(locator.First)
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeoutMs });

        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            if (await locator.CountAsync() >= min)
                return;

            await locator.Page.WaitForTimeoutAsync(200);
        }

        throw new TimeoutException(
            $"Locator matched < {min} elements after {timeoutMs} ms.");
    }

    public static async Task<bool> IsVisibleWithinAsync(this ILocator locator, int timeoutMs = 1000)
    {
        try
        {
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeoutMs
            });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    public static ILocator ProjectRow(this IPage page, string projectName, bool inFavourites)
    {
        var rowXpath = inFavourites
            ? $"//div[contains(@class,'v-expansion-panel--active')]//table//tbody//tr[td[2]//div[normalize-space(text())='{projectName}']]"
            : $"//div[@class='v-card-text']//table//tbody//tr[td[2]//div[normalize-space(text())='{projectName}']]";

        return page.Locator(rowXpath);
    }
}