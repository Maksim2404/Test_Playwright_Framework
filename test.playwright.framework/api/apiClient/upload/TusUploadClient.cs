using System.Text;
using System.Text.Json;
using Allure.Net.Commons;
using test.playwright.framework.api.auth;
using test.playwright.framework.api.config;

namespace test.playwright.framework.api.apiClient.upload;

public sealed class TusUploadClient(HttpClient http, TokenProvider tokenProvider, ApiConfig apiCfg)
    : BaseApiClient(http, tokenProvider, apiCfg)
{
    private const string TusPath = "/api/v1/tus-upload";
    private const string TusVersion = "1.0.0";

    public async Task<Uri> CreateUploadAsync(string fileName, string contentType, long length, ApiUserKind userKind,
        int userId, int fileTypeId)
    {
        return await AllureApi.Step($"TUS | Create upload '{fileName}' ({length} bytes)", async () =>
        {
            var auth = await CreateAuthorizedJsonRequestAsync(HttpMethod.Post, TusPath, body: null, userKind: userKind);

            var request = new HttpRequestMessage(HttpMethod.Post, TusPath);
            request.Headers.Authorization = auth.Headers.Authorization;
            request.Headers.TryAddWithoutValidation("Tus-Resumable", TusVersion);
            request.Headers.TryAddWithoutValidation("Upload-Length", length.ToString());

            var safeFileName = Path.GetFileName(fileName);
            var uploadRequestJson = JsonSerializer.Serialize(new { userId, fileTypeId });

            var metadata = new Dictionary<string, string>
            {
                ["filename"] = safeFileName,
                ["filetype"] = contentType,
                ["uploadRequest"] = uploadRequestJson
            };

            request.Headers.TryAddWithoutValidation("Upload-Metadata", EncodeMetadata(metadata));
            request.Content = new ByteArrayContent([]);

            var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                await AllureAttachRequestAsync(request, "TUS Create");
                await AllureAttachResponseAsync(response, "TUS Create");
                var raw = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"TUS create failed: {(int)response.StatusCode} {response.StatusCode}. Body: {raw}");
            }

            var location = response.Headers.Location
                           ?? throw new InvalidOperationException(
                               "TUS create succeeded but Location header is missing");

            return location.IsAbsoluteUri
                ? location
                : new Uri(new Uri(ApiCfg.ApiBaseUrl.TrimEnd('/')), location);
        });
    }

    public async Task UploadAllAtOnceAsync(Uri uploadUrl, byte[] bytes, ApiUserKind userKind)
    {
        await AllureApi.Step($"TUS | Upload {bytes.Length} bytes", async () =>
        {
            var auth = await CreateAuthorizedJsonRequestAsync(HttpMethod.Get, TusPath, body: null, userKind: userKind);
            var patch = new HttpRequestMessage(new HttpMethod("PATCH"), uploadUrl);
            patch.Headers.Authorization = auth.Headers.Authorization;

            patch.Headers.TryAddWithoutValidation("Tus-Resumable", TusVersion);
            patch.Headers.TryAddWithoutValidation("Upload-Offset", "0");

            patch.Content = new ByteArrayContent(bytes);
            patch.Content.Headers.TryAddWithoutValidation("Content-Type", "application/offset+octet-stream");

            var response = await Http.SendAsync(patch);
            if (!response.IsSuccessStatusCode)
            {
                await AllureAttachRequestAsync(patch, "TUS PATCH");
                await AllureAttachResponseAsync(response, "TUS PATCH");
                var raw = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"TUS upload failed: {(int)response.StatusCode} {response.StatusCode}. Body: {raw}");
            }
        });
    }

    private static string EncodeMetadata(IReadOnlyDictionary<string, string> metadata)
        => string.Join(",", metadata.Select(kv =>
            $"{kv.Key} {Convert.ToBase64String(Encoding.UTF8.GetBytes(kv.Value))}"));
}