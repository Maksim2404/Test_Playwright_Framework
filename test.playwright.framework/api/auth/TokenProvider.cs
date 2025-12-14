using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using test.playwright.framework.api.config;

namespace test.playwright.framework.api.auth;

public sealed class TokenProvider(HttpClient http, ApiConfig cfg)
{
    private sealed class CachedToken
    {
        public string? AccessToken { get; init; }
        public DateTime ExpiresAtUtc { get; init; }
    }

    private readonly ConcurrentDictionary<string, CachedToken> _tokenCache = new();

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; init; } = null!;
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }
    }

    /// <summary>
    /// Get an access token for the default API user (from ApiConfig).
    /// </summary>
    public Task<string> GetAccessTokenAsync() => GetAccessTokenAsync(cfg.Username, cfg.Password);

    /// <summary>
    /// Get access token for a specific user (username/password).
    /// Useful for role/permission tests with non-admin users.
    /// </summary>
    public async Task<string> GetAccessTokenAsync(string username, string password)
    {
        var cacheKey = username.Trim().ToLowerInvariant();

        if (_tokenCache.TryGetValue(cacheKey, out var cached) && !string.IsNullOrEmpty(cached.AccessToken) &&
            cached.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(1))
        {
            return cached.AccessToken!;
        }

        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "password"),
            new("client_id", cfg.ClientId),
            new("username", username),
            new("password", password)
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, cfg.TokenUrl);
        request.Content = new FormUrlEncodedContent(form);

        using var response = await http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Token request failed for user '{username}': {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>()
                      ?? throw new InvalidOperationException("Token response is empty or invalid JSON");

        var token = payload.AccessToken;
        var expiresAt = DateTime.UtcNow.AddSeconds(payload.ExpiresIn);

        _tokenCache[cacheKey] = new CachedToken
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAt
        };

        return token;
    }
}