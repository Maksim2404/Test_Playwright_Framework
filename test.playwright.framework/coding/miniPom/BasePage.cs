using Microsoft.Playwright;

namespace test.playwright.framework.coding.miniPom;

public abstract class BasePage(IPage page)
{
    protected IPage Page { get; } = page;

    public async Task GoToAsync(string url)
    {
        await Page.GotoAsync(url);
    }

    public async Task ClickAsync(ILocator locator)
    {
        await locator.ClickAsync();
    }

    public async Task FillAsync(ILocator locator, string text)
    {
        await locator.FillAsync(text);
    }

    public async Task<string> GetTextAsync(ILocator locator)
    {
        return await locator.InnerTextAsync();
    }

    public async Task SafeClickAsync(ILocator locator, int retries = 3)
    {
        var attempts = 0;
        while (attempts < retries)
        {
            try
            {
                await locator.ClickAsync();
                return;
            }
            catch (PlaywrightException ex)
            {
                attempts++;
                if (attempts == retries)
                    throw new Exception($"Failed to click after {retries} attempts: {ex.Message}");
                await Task.Delay(500);
            }
        }
    }
}