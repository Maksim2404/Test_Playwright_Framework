using Microsoft.Extensions.Configuration;

namespace test.playwright.framework.utils.retry;

public static class RetryConfig
{
    private static readonly IConfigurationRoot Config;

    static RetryConfig()
    {
        Config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
    }

    public static int GlobalRetryCount
    {
        get
        {
            var raw = Config["GlobalRetryCount"];
            Console.WriteLine($"[RetryConfig] Found GlobalRetryCount = {raw}");
            return int.TryParse(raw, out var parsed) ? parsed : 1;
        }
    }
}