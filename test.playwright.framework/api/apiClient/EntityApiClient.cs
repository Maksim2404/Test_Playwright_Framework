using System.Net.Http.Json;
using Allure.Net.Commons;
using Serilog;
using test.playwright.framework.api.auth;
using test.playwright.framework.api.config;
using test.playwright.framework.api.dto.entity_1;

namespace test.playwright.framework.api.apiClient;

public sealed class EntityApiClient(HttpClient http, TokenProvider tokenProvider, ApiConfig apiCfg)
    : BaseApiClient(http, tokenProvider, apiCfg)
{
    private const string BasePath = "/api/v1/entity";

    public async Task<EntitySearchResponse> SearchAsync(string? searchTerm, bool? showDeleted, bool showInactive,
        int pageSize = 20, int pageIndex = 0)
    {
        return await AllureApi.Step($"Entity: Search entities (term='{searchTerm}', showDeleted={showDeleted}, " +
                                    $"showInactive={showInactive}, pageSize={pageSize}, pageIndex={pageIndex})",
            async () =>
            {
                var body = new
                {
                    searchPage = new
                    {
                        pageSize,
                        pageIndex
                    },
                    sort = (string?)null,
                    searchTerm = searchTerm,
                    showDeleted = showDeleted,
                    showInactive = showInactive
                };

                var request =
                    await CreateAuthorizedJsonRequestAsync(HttpMethod.Post, $"{BasePath}/entity-search", body);

                var response = await Http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var dto = await response.Content.ReadFromJsonAsync<EntitySearchResponse>()
                          ?? throw new InvalidOperationException("Failed to deserialize EntitySearchResponse");

                return dto;
            });
    }

    public async Task<EntityRecord> CreateAsync(string name)
    {
        return await AllureApi.Step($"Entity: Create entity '{name}'", async () =>
        {
            var body = new { entityName = name, activeFlag = true };
            var request = await CreateAuthorizedJsonRequestAsync(HttpMethod.Post, BasePath, body);
            var response = await Http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            EntityRecord? created = null;
            try
            {
                created = await response.Content.ReadFromJsonAsync<EntityRecord>();
            }
            catch
            {
                // ignore, in case API return empty body on create
            }

            if (created is not null) return created;

            var search = await SearchAsync(name, showDeleted: null, showInactive: false, pageSize: 50);
            var match = search.Records.FirstOrDefault(l =>
                string.Equals(l.EntityName, name, StringComparison.OrdinalIgnoreCase));

            if (match is null) throw new InvalidOperationException("Entity was created but not found via search.");

            return match;
        });
    }

    public async Task<EntityRecord> UpdateAsync(int entityId, string name, bool activeFlag)
    {
        return await AllureApi.Step($"Entity: Update entity ID={entityId} to '{name}', active={activeFlag}",
            async () =>
            {
                var body = new { entityName = name, activeFlag = activeFlag };
                var request = await CreateAuthorizedJsonRequestAsync(HttpMethod.Put, $"{BasePath}/{entityId}", body);

                var response = await Http.SendAsync(request);
                var rawBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    await AllureAttachRequestAsync(request, "Update Entity");
                    await AllureAttachResponseAsync(response, "Update Entity");
                    await AttachOnFailureAsync(response, "Update Entity");
                    Log.Warning("Update failed. Status: {StatusCode} {Status}. Body: {Body}",
                        (int)response.StatusCode, response.StatusCode,
                        string.IsNullOrWhiteSpace(rawBody) ? "<empty>" : rawBody);

                    throw new HttpRequestException(
                        $"Entity update failed with {(int)response.StatusCode} {response.StatusCode}. Body: {rawBody}");
                }

                EntityRecord? updated = null;
                try
                {
                    updated = await response.Content.ReadFromJsonAsync<EntityRecord>();
                }
                catch
                {
                    // fallback to search
                }

                if (updated is not null) return updated;

                var search = await SearchAsync(searchTerm: name, showDeleted: null, showInactive: false, pageSize: 50);
                var match = search.Records.FirstOrDefault(c => c.EntityId == entityId &&
                                                               string.Equals(c.EntityName, name,
                                                                   StringComparison.OrdinalIgnoreCase));

                if (match is null)
                    throw new InvalidOperationException("Entity was updated but not found via search.");

                return match;
            });
    }

    public async Task<EntityRecord?> FindByNameAsync(string name, bool includeInactive = false,
        bool? showDeleted = null)
    {
        return await AllureApi.Step($"Entity: Find by name '{name}' (includeInactive={includeInactive}, " +
                                    $"showDeleted={showDeleted})", async () =>
        {
            var search = await SearchAsync(searchTerm: name, showDeleted: showDeleted, showInactive: includeInactive,
                pageSize: 50);

            return search.Records.FirstOrDefault(c =>
                string.Equals(c.EntityName, name, StringComparison.OrdinalIgnoreCase));
        });
    }

    public async Task SetActiveAsync(EntityRecord record, bool active)
    {
        await AllureApi.Step($"Entity: Set entity '{record.EntityName}' (ID={record.EntityId}) active={active}",
            async () =>
            {
                var body = new
                {
                    entityName = record.EntityName,
                    activeFlag = active
                };

                var request =
                    await CreateAuthorizedJsonRequestAsync(HttpMethod.Put, $"{BasePath}/{record.EntityId}", body);

                var response = await Http.SendAsync(request);
                response.EnsureSuccessStatusCode();
            });
    }

    public async Task DeleteAsync(int languageId)
    {
        await AllureApi.Step($"Entity: Soft-delete entity ID={languageId}", async () =>
        {
            var request = await CreateAuthorizedJsonRequestAsync(HttpMethod.Delete, $"{BasePath}/{languageId}");

            var response = await Http.SendAsync(request);
            response.EnsureSuccessStatusCode();
        });
    }

    public async Task UndeleteAsync(int languageId)
    {
        await AllureApi.Step($"Entity: Undelete entity ID={languageId}", async () =>
        {
            var body = new { undelete = true };
            var request = await CreateAuthorizedJsonRequestAsync(HttpMethod.Delete,
                $"{BasePath}/{languageId}?undelete=true", body);

            var response = await Http.SendAsync(request);
            response.EnsureSuccessStatusCode();
        });
    }
}