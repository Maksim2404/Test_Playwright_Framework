using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Allure.Net.Commons;
using FluentAssertions;
using NUnit.Framework;
using Serilog;
using test.playwright.framework.api.auth;
using test.playwright.framework.api.common;
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
    
    protected static readonly JsonSerializerOptions IgnoreNulls = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<HttpRequestMessage> CreateAuthorizedJsonRequestAsync(HttpMethod method, string relativePath,
        object? body = null, ApiUserKind userKind = ApiUserKind.Admin, JsonSerializerOptions? jsonOptions = null)
    {
        var token = await GetTokenForUserKindAsync(userKind);

        var request = new HttpRequestMessage(method, relativePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (body is not null) request.Content = JsonContent.Create(body, options: jsonOptions);

        return request;
    }

    protected static async Task AttachOnFailureAsync(HttpResponseMessage response, string namePrefix = "API")
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();

        AllureApi.AddAttachment($"{namePrefix} Response", "application/json",
            string.IsNullOrWhiteSpace(body) ? "<empty body>" : body);
    }
    
    private static string SanitizeSensitive(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;

        s = Regex.Replace(s, @"(?im)^Authorization:\s*Bearer\s+.+$", "Authorization: <redacted>");
        s = Regex.Replace(s, @"(?im)^Cookie:\s*.+$", "Cookie: <redacted>");
        s = Regex.Replace(s, @"(?im)^Set-Cookie:\s*.+$", "Set-Cookie: <redacted>");
        s = Regex.Replace(s, @"(?im)^Upload-Metadata:\s*.+$", "Upload-Metadata: <redacted>");

        s = Regex.Replace(s, "\"access_token\"\\s*:\\s*\"[^\"]*\"", "\"access_token\":\"<redacted>\"");
        s = Regex.Replace(s, "\"refresh_token\"\\s*:\\s*\"[^\"]*\"", "\"refresh_token\":\"<redacted>\"");
        s = Regex.Replace(s, "\"id_token\"\\s*:\\s*\"[^\"]*\"", "\"id_token\":\"<redacted>\"");

        return s;
    }

    private static void AllureAttachText(string name, string? content, string contentType = "text/plain",
        string ext = ".txt")
    {
        var safeName = AllureEnvironmentWriter.Safe(name);
        var sanitized = SanitizeSensitive(content ?? string.Empty);
        var bytes = Encoding.UTF8.GetBytes(sanitized);

        AllureApi.AddAttachment(safeName, contentType, bytes, ext);
    }

    protected static async Task AllureAttachRequestAsync(HttpRequestMessage request, string prefix)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{request.Method} {request.RequestUri}");

        foreach (var h in request.Headers)
        {
            if (h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("Authorization: <redacted>");
                continue;
            }

            if (h.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("Cookie: <redacted>");
                continue;
            }

            if (h.Key.Equals("Upload-Metadata", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("Upload-Metadata: <redacted>");
                continue;
            }

            sb.AppendLine($"{h.Key}: {string.Join(", ", h.Value)}");
        }

        if (request.Content is not null)
        {
            foreach (var h in request.Content.Headers)
                sb.AppendLine($"{h.Key}: {string.Join(", ", h.Value)}");

            var body = await request.Content.ReadAsStringAsync();
            sb.AppendLine();
            sb.AppendLine(body);
        }

        AllureAttachText($"{AllureEnvironmentWriter.Safe(prefix)} - Request", sb.ToString(), ext: ".txt");
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

        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
        var looksJson = mediaType.Contains("json", StringComparison.OrdinalIgnoreCase);

        AllureAttachText($"{AllureEnvironmentWriter.Safe(prefix)} - Response", sb.ToString(),
            looksJson ? "application/json" : "text/plain", looksJson ? ".json" : ".txt");
    }

    public async Task<HttpResponseMessage> SendAdminAuthorizedAsync(HttpRequestMessage request)
    {
        var token = await TokenProvider.GetAccessTokenAsync();
        token.Should().NotBeNullOrWhiteSpace("Admin token must be available for negative tests.");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await Http.SendAsync(request);
    }

    public async Task<HttpResponseMessage> SendPublicAuthorizedAsync(HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(ApiCfg.PublicUsername) || string.IsNullOrWhiteSpace(ApiCfg.PublicPassword))
            Assert.Ignore("Public (non-admin) user credentials are not configured in ApiConfig.");

        var token = await TokenProvider.GetAccessTokenAsync(ApiCfg.PublicUsername, ApiCfg.PublicPassword);
        token.Should().NotBeNullOrWhiteSpace("Public token must be available for negative tests.");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await Http.SendAsync(request);
    }

    public async Task<HttpResponseMessage> AssertDeniedAsync(HttpRequestMessage request,
        HttpResponseMessage response, string stepName = "PERM Negative")
    {
        if (response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden))
        {
            await AllureAttachRequestAsync(request, stepName);
            await AllureAttachResponseAsync(response, stepName);

            var raw = await response.Content.ReadAsStringAsync();
            Log.Error("Unexpected status {Status}. Body: {Body}", response.StatusCode, raw);
        }

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
        return response;
    }
}