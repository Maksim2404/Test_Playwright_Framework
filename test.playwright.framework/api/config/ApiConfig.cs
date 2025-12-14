using Microsoft.Extensions.Configuration;
using test.playwright.framework.fixtures.config;

namespace test.playwright.framework.api.config;

public sealed class ApiConfig
{
    public required string ApiBaseUrl { get; init; }
    public required string TokenUrl { get; init; }
    public required string ClientId { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public string? PublicUsername { get; init; }
    public string? PublicPassword { get; init; }

    public static (ApiConfig apiCfg, IConfiguration rawConfig, string env) Read(IConfiguration config)
    {
        var envFromRun = NUnit.Framework.TestContext.Parameters.Get("env");
        var envVar = config["TEST_ENV"];
        var env = (AtfConfig.TestEnv.Override ?? envFromRun ?? envVar ?? "qa").Trim().ToLowerInvariant();

        var apiBaseUrl = config[$"test:api:playwright:Urls:{env}"]
                         ?? throw new InvalidOperationException($"No API URL configured for env '{env}'");

        var tokenUrl = config["test:api:playwright:Auth:TokenUrl"]
                       ?? throw new InvalidOperationException("api:Auth:TokenUrl is missing");

        var clientId = config["test:api:playwright:Auth:ClientId"]
                       ?? throw new InvalidOperationException("api:Auth:ClientId is missing");

        var username = config["test:api:playwright:Auth:Username"]
                       ?? throw new InvalidOperationException("api:Auth:Username is missing");

        var password = config["test:api:playwright:Auth:Password"]
                       ?? throw new InvalidOperationException("api:Auth:Password is missing");
        
        var publicUsername = config["test:api:playwright:Auth:PublicUsername"];
        var publicPassword = config["test:api:playwright:Auth:PublicPassword"];

        var cfg = new ApiConfig
        {
            ApiBaseUrl = apiBaseUrl.TrimEnd('/') + "/",
            TokenUrl = tokenUrl,
            ClientId = clientId,
            Username = username,
            Password = password,
            PublicUsername = publicUsername,
            PublicPassword = publicPassword
        };

        return (cfg, config, env);
    }
}