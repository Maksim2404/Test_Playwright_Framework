using Microsoft.Playwright;
using test.playwright.framework.pages.enums;

namespace test.playwright.framework.utils.interfaces;

public interface IBrowserFactory : IAsyncDisposable
{
    Task ApplyThrottlingAsync(IBrowserContext context, NetworkPreset networkType);
    Task<IBrowser> GetBrowserAsync();
    Task<IBrowserContext> CreateContextAsync(ViewportSize? viewport = null, bool ignoreHttps = true);
    Task<IPage> CreatePageAsync(IBrowserContext ctx, NetworkPreset preset = NetworkPreset.None);
    Task SetOfflineAsync(IBrowserContext ctx, bool offline);
}
