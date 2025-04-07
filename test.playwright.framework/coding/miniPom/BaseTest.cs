using Microsoft.Playwright;
using NUnit.Framework;

namespace test.playwright.framework.coding.miniPom;

public abstract class BaseTest
{
    private IBrowser _browser;
    private IBrowserContext _context;
    protected IPage Page;
    private IPlaywright _playwright;
    
    [SetUp]
    public async Task SetUp()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
        _context = await _browser.NewContextAsync();
        Page = await _context.NewPageAsync();
    }
    
    [TearDown]
    public async Task TearDown()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }
}