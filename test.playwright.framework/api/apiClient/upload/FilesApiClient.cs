using System.Net.Http.Json;
using System.Text.Json;
using Allure.Net.Commons;
using test.playwright.framework.api.auth;
using test.playwright.framework.api.config;
using test.playwright.framework.api.dto.entity_1.files;

namespace test.playwright.framework.api.apiClient.upload;

public sealed record DownloadedFile(byte[] Bytes, string? FileName, string? ContentType);

public sealed class FilesApiClient(HttpClient http, TokenProvider tokenProvider, ApiConfig apiCfg)
    : BaseApiClient(http, tokenProvider, apiCfg)
{
    private const string BasePath = "/api/v1/file-upload";

    public async Task<FilesRecord> GetByIdAsync(int id)
    {
        return await AllureApi.Step($"Files | Get by id {id}", async () =>
        {
            var request = await CreateAuthorizedJsonRequestAsync(HttpMethod.Get, $"{BasePath}/{id}");
            var response = await Http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                await AllureAttachRequestAsync(request, "Files Get by id");
                await AllureAttachResponseAsync(response, "Files Get by id");
                var raw = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Get Docs by id failed: {(int)response.StatusCode} {response.StatusCode}. Body: {raw}");
            }

            return await response.Content.ReadFromJsonAsync<FilesRecord>()
                   ?? throw new InvalidOperationException("Failed to deserialize FilesRecord");
        });
    }

    public async Task<FilesRecord> UploadAndWaitForFileAsync(TusUploadClient tus, int userId, string fileName,
        string contentType, byte[] bytes, ApiUserKind userKind, int docTypeId, int timeoutSeconds = 10)
    {
        return await AllureApi.Step(
            $"Uploading File to profile {userId} using fileTypeId={docTypeId}, file={fileName}", async () =>
            {
                var startedAt = DateTime.UtcNow;

                var uploadUrl = await tus.CreateUploadAsync(fileName, contentType, bytes.LongLength, userKind,
                    userId, docTypeId);
                await tus.UploadAllAtOnceAsync(uploadUrl, bytes, userKind);

                var deadline = DateTimeOffset.UtcNow.AddSeconds(timeoutSeconds);

                while (DateTimeOffset.UtcNow < deadline)
                {
                    var byName = await SearchAsync(userId, fileName, showInactive: null,
                        showDeleted: null);

                    var foundByName = byName.Records
                        .Where(d => d.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(d => d.CreatedDate)
                        .FirstOrDefault();

                    if (foundByName is not null) return foundByName;

                    var recent = await SearchAsync(userId);

                    var foundFallback = recent.Records
                        .Where(d => d.FileType.FileTypeId == docTypeId)
                        .Where(d => d.Length == bytes.Length)
                        .Where(d => d.CreatedDate.ToUniversalTime() >= startedAt.AddSeconds(-5))
                        .OrderByDescending(d => d.CreatedDate)
                        .FirstOrDefault();

                    if (foundFallback is not null) return foundFallback;

                    await Task.Delay(500);
                }

                throw new TimeoutException(
                    $"Uploaded '{fileName}' but it did not appear in user {userId} within {timeoutSeconds}s");
            });
    }

    public async Task<DownloadedFile> GetFileContentAsync(int fileId, bool download = true)
    {
        return await AllureApi.Step($"Files | File content fileId={fileId} download={download}", async () =>
        {
            var request = await CreateAuthorizedJsonRequestAsync(
                HttpMethod.Get, $"{BasePath}/{fileId}/file-content?download={download.ToString().ToLowerInvariant()}");

            var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                await AllureAttachRequestAsync(request, "Files FileContent");
                await AllureAttachResponseAsync(response, "Files FileContent");
                var raw = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"File-content failed: {(int)response.StatusCode} {response.StatusCode}. Body: {raw}");
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();

            var contentType = response.Content.Headers.ContentType?.ToString();
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"');

            fileName ??= response.Content.Headers.TryGetValues("Content-Disposition", out var cd)
                ? cd.FirstOrDefault()?.Split("filename=").LastOrDefault()?.Trim().Trim('"')
                : null;

            return new DownloadedFile(bytes, fileName, contentType);
        });
    }

    public async Task<FilesSearchResponse> SearchAsync(int? userId = null, string? searchTerm = null,
        IReadOnlyCollection<int>? fileTypes = null, bool? showInactive = null, bool? showDeleted = null,
        int pageSize = 20, int pageIndex = 0, string? sortColumn = null, string? sortDirection = null)
    {
        return await AllureApi.Step($"Files | Search term='{searchTerm}'", async () =>
        {
            var body = new
            {
                searchPage = new { pageSize, pageIndex },
                sort = sortColumn is null ? null : new { sortColumn, sortDirection = sortDirection ?? "Ascending" },
                searchTerm,
                userId,
                fileTypes = fileTypes,
                showDeleted,
                showInactive
            };

            var request = await CreateAuthorizedJsonRequestAsync(HttpMethod.Post,
                $"{BasePath}/user-files-search", body, jsonOptions: IgnoreNulls);
            var response = await Http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return JsonSerializer.Deserialize<FilesSearchResponse>(raw)
                       ?? throw new InvalidOperationException("Failed to deserialize FilesSearchResponse");
            await AllureAttachRequestAsync(request, "Files Search");
            await AllureAttachResponseAsync(response, "Files Search");
            throw new HttpRequestException(
                $"Search Files failed: {(int)response.StatusCode} {response.StatusCode}. Body: {raw}.");
        });
    }

    public async Task<FilesRecord> UpdateAsync(int id, object updateBody)
    {
        return await AllureApi.Step($"Files | Update ID={id}", async () =>
        {
            var request = await CreateAuthorizedJsonRequestAsync(HttpMethod.Put, $"{BasePath}/{id}", updateBody);

            var response = await Http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await AllureAttachRequestAsync(request, "Files Update");
                await AllureAttachResponseAsync(response, "Files Update");
                throw new HttpRequestException(
                    $"Update failed: {(int)response.StatusCode} {response.StatusCode}. Body: {raw}");
            }

            FilesRecord? updated = null;
            try
            {
                updated = await response.Content.ReadFromJsonAsync<FilesRecord>();
            }
            catch
            {
                // fallback to search
            }

            if (updated is not null) return updated;

            var match = await GetByIdAsync(id);
            if (match is null)
                throw new InvalidOperationException($"Files '{id}' updated but not found via search.");

            return match;
        });
    }

    public Task<FilesRecord> UpdateAsync(int fileId, int fileTypeId, string fileName, string fileNotes)
    {
        var body = new
        {
            originalFileName = fileName,
            documentTypeId = fileTypeId,
            talentProfileDocumentNotes = fileNotes,
            activeFlag = true
        };

        return UpdateAsync(fileId, body);
    }

    public async Task<FilesRecord?> FindByFileNameAsync(int? userId, string fileName,
        bool? showInactive = null, bool? showDeleted = null)
    {
        return await AllureApi.Step($"Files: Find by file name '{fileName}' showInactive={showInactive}, " +
                                    $"showDeleted={showDeleted}", async () =>
        {
            var search = await SearchAsync(userId, fileName, showInactive: showInactive,
                showDeleted: showDeleted, pageSize: 50);

            return search.Records.FirstOrDefault(d =>
                string.Equals(d.FileName, fileName, StringComparison.OrdinalIgnoreCase));
        });
    }

    public async Task DeleteAsync(int id)
    {
        await AllureApi.Step($"Files: Soft-delete doc ID={id}", async () =>
        {
            var request = await CreateAuthorizedJsonRequestAsync(HttpMethod.Delete, $"{BasePath}/{id}");

            var response = await Http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                await AllureAttachRequestAsync(request, "Files Delete");
                await AllureAttachResponseAsync(response, "Files Delete");
                throw new HttpRequestException($"Delete failed... Body: {raw}");
            }
        });
    }

    public async Task UndeleteAsync(int id)
    {
        await AllureApi.Step($"Files: Undelete doc ID={id}", async () =>
        {
            var body = new { undelete = true };
            var request =
                await CreateAuthorizedJsonRequestAsync(HttpMethod.Delete, $"{BasePath}/{id}?undelete=true", body);

            var response = await Http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                await AllureAttachRequestAsync(request, "Files Undelete");
                await AllureAttachResponseAsync(response, "Files Undelete");
                throw new HttpRequestException($"Undelete failed... Body: {raw}");
            }
        });
    }

    public async Task<IReadOnlyList<FilesRecord>> BulkUpdateFileTypeAsync(int newFileTypeId, params int[] filesIds)
    {
        return await AllureApi.Step($"Files: Bulk Update File.Type ({filesIds.Length})", async () =>
        {
            var body = new { fileIds = filesIds, fileTypeId = newFileTypeId };

            var request =
                await CreateAuthorizedJsonRequestAsync(HttpMethod.Patch, $"{BasePath}/bulk-update-file-type", body);
            var response = await Http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await AllureAttachRequestAsync(request, "File Type BulkUpdate");
                await AllureAttachResponseAsync(response, "File Type BulkUpdate");
                throw new HttpRequestException(
                    $"BulkUpdate failed: {(int)response.StatusCode} {response.StatusCode}. Body: {raw}");
            }

            try
            {
                var returned = await response.Content.ReadFromJsonAsync<List<FilesRecord>>();
                if (returned is { Count: > 0 }) return returned;
            }
            catch
            {
                // fallback to search
            }

            var result = new List<FilesRecord>();
            foreach (var id in filesIds)
            {
                var found = await GetByIdAsync(id);
                if (found is null)
                    throw new InvalidOperationException($"BulkUpdate succeeded but '{id}' not found via search.");
                result.Add(found);
            }

            return result;
        });
    }

    public async Task<IReadOnlyList<FilesRecord>> BulkDeleteAsync(params int[] filesIds)
    {
        return await AllureApi.Step($"Files: Bulk Delete ({filesIds.Length})", async () =>
        {
            var body = new { talentProfileDocumentIds = filesIds };

            var request =
                await CreateAuthorizedJsonRequestAsync(HttpMethod.Delete, $"{BasePath}/bulk-delete", body);
            var response = await Http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await AllureAttachRequestAsync(request, "Files BulkDelete");
                await AllureAttachResponseAsync(response, "Files BulkDelete");
                throw new HttpRequestException(
                    $"BulkDelete failed: {(int)response.StatusCode} {response.StatusCode}. Body: {raw}");
            }

            try
            {
                var returned = await response.Content.ReadFromJsonAsync<List<FilesRecord>>();
                if (returned is { Count: > 0 }) return returned;
            }
            catch
            {
                // fallback to search
            }

            var result = new List<FilesRecord>();
            foreach (var id in filesIds)
            {
                var found = await GetByIdAsync(id);
                if (found is null)
                    throw new InvalidOperationException($"BulkDelete succeeded but '{id}' not found via search.");
                result.Add(found);
            }

            return result;
        });
    }

    public async Task<IReadOnlyList<FilesRecord>> BulkUndeleteAsync(params int[] filesIds)
    {
        return await AllureApi.Step($"Files: Bulk Undelete ({filesIds.Length})", async () =>
        {
            var body = new { talentProfileDocumentIds = filesIds };

            var request = await CreateAuthorizedJsonRequestAsync(HttpMethod.Delete,
                $"{BasePath}/bulk-delete?undelete=true", body);
            var response = await Http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await AllureAttachRequestAsync(request, "Files BulkUndelete");
                await AllureAttachResponseAsync(response, "Files BulkUndelete");
                throw new HttpRequestException(
                    $"BulkUndelete failed: {(int)response.StatusCode} {response.StatusCode}. Body: {raw}");
            }

            try
            {
                var returned = await response.Content.ReadFromJsonAsync<List<FilesRecord>>();
                if (returned is { Count: > 0 }) return returned;
            }
            catch
            {
                // fallback to search
            }

            var result = new List<FilesRecord>();
            foreach (var id in filesIds)
            {
                var found = await GetByIdAsync(id);
                if (found is null)
                    throw new InvalidOperationException($"BulkUndelete succeeded but '{id}' not found via search.");
                result.Add(found);
            }

            return result;
        });
    }
}