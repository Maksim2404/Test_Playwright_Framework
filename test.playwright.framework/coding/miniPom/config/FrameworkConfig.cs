namespace test.playwright.framework.coding.miniPom.config;

public static class FrameworkConfig
{
    public static string BaseUrl = "https://example.com";
    public static readonly BrowserType Browser = BrowserType.Chromium;
}

public enum BrowserType
{
    Chromium,
    Firefox,
    Webkit
}