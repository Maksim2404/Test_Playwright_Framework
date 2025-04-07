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
}
