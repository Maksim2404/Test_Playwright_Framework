using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Allure.Net.Commons;
using test.playwright.framework.api.auth;
using test.playwright.framework.api.config;

namespace test.playwright.framework.api.apiClient;

public abstract class BaseApiClient(HttpClient http, TokenProvider tokenProvider, ApiConfig apiCfg)
{
    protected HttpClient Http { get; } = http;
    protected TokenProvider TokenProvider { get; } = tokenProvider;
    protected ApiConfig ApiCfg { get; } = apiCfg;

    private async Task<string> GetTokenForUserKindAsync(ApiUserKind kind)
    {
        switch (kind)
        {
            case ApiUserKind.Admin: return await TokenProvider.GetAccessTokenAsync();

            case ApiUserKind.Public:
                if (string.IsNullOrWhiteSpace(ApiCfg.PublicUsername) ||
                    string.IsNullOrWhiteSpace(ApiCfg.PublicPassword))
                {
                    throw new InvalidOperationException(
                        "Public user is not configured (test:api:playwright:Auth:PublicUsername/PublicPassword).");
                }

                return await TokenProvider.GetAccessTokenAsync(ApiCfg.PublicUsername!, ApiCfg.PublicPassword!);

            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported ApiUserKind");
        }
    }

    public async Task<HttpRequestMessage> CreateAuthorizedJsonRequestAsync(HttpMethod method, string relativePath,
        object? body = null, ApiUserKind userKind = ApiUserKind.Admin)
    {
        var token = await GetTokenForUserKindAsync(userKind);

        var request = new HttpRequestMessage(method, relativePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (body is not null) request.Content = JsonContent.Create(body);

        return request;
    }

    protected static async Task AttachOnFailureAsync(HttpResponseMessage response, string namePrefix = "API")
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();

        AllureApi.AddAttachment($"{namePrefix} Response", "application/json",
            string.IsNullOrWhiteSpace(body) ? "<empty body>" : body);
    }

    private static void AllureAttachText(string name, string content, string contentType = "text/plain")
    {
        AllureApi.AddAttachment(name, contentType, content);
    }

    protected static async Task AllureAttachRequestAsync(HttpRequestMessage request, string prefix)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"{request.Method} {request.RequestUri}");
        foreach (var h in request.Headers)
            sb.AppendLine($"{h.Key}: {string.Join(", ", h.Value)}");

        if (request.Content is not null)
        {
            foreach (var h in request.Content.Headers)
                sb.AppendLine($"{h.Key}: {string.Join(", ", h.Value)}");

            var body = await request.Content.ReadAsStringAsync();
            sb.AppendLine();
            sb.AppendLine(body);
        }

        var txt = sb.ToString().Replace("Authorization: Bearer ", "Authorization: Bearer <redacted> ");
        AllureAttachText($"{prefix} - Request", txt);
    }

    protected static async Task AllureAttachResponseAsync(HttpResponseMessage response, string prefix)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"HTTP {(int)response.StatusCode} {response.StatusCode}");
        foreach (var h in response.Headers)
            sb.AppendLine($"{h.Key}: {string.Join(", ", h.Value)}");


        foreach (var h in response.Content.Headers)
            sb.AppendLine($"{h.Key}: {string.Join(", ", h.Value)}");

        var body = await response.Content.ReadAsStringAsync();
        sb.AppendLine();
        sb.AppendLine(string.IsNullOrWhiteSpace(body) ? "<empty body>" : body);

        AllureAttachText($"{prefix} - Response", sb.ToString());
    }
    
}