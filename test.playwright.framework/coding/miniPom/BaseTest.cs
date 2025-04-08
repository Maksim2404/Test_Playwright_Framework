using Microsoft.Playwright;
using NUnit.Framework;
using test.playwright.framework.coding.miniPom.config;
using BrowserType = test.playwright.framework.coding.miniPom.config.BrowserType;

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
        _browser = FrameworkConfig.Browser switch
        {
            BrowserType.Chromium => await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                { Headless = false }),
            BrowserType.Firefox => await _playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
                { Headless = false }),
            BrowserType.Webkit => await _playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions
                { Headless = false }),
            _ => _browser
        };

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