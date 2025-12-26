using System.Net.Http.Json;
using Allure.Net.Commons;
using test.playwright.framework.api.auth;
using test.playwright.framework.api.config;
using test.playwright.framework.api.dto.entity_1;

namespace test.playwright.framework.api.apiClient;

public sealed class EntityApiClient(HttpClient http, TokenProvider tokenProvider, ApiConfig apiCfg)
    : BaseApiClient(http, tokenProvider, apiCfg)
{
    private const string BasePath = "/api/v1/entity";

    public async Task<EntityDetailsDto> GetByIdAsync(int id)
    {
        return await AllureApi.Step($"Entity | Get by id {id}", async () =>
        {
            var request = await CreateAuthorizedJsonRequestAsync(HttpMethod.Get, $"{BasePath}/{id}");
            var response = await Http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                await AllureAttachRequestAsync(request, "Entity Get by id");
                await AllureAttachResponseAsync(response, "Entity Get by id");
                var raw = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Get Entity by id failed: {(int)response.StatusCode} {response.StatusCode}. Body: {raw}");
            }

            return await response.Content.ReadFromJsonAsync<EntityDetailsDto>()
                   ?? throw new InvalidOperationException("Failed to deserialize EntityDetailsDto");
        });
    }

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
                    searchTerm,
                    showDeleted,
                    showInactive
                };

                var request =
                    await CreateAuthorizedJsonRequestAsync(HttpMethod.Post, $"{BasePath}/entity-search", body);

                var response = await Http.SendAsync(request);
                var raw = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<EntitySearchResponse>()
                           ?? throw new InvalidOperationException("Failed to deserialize EntitySearchResponse");
                await AllureAttachRequestAsync(request, "Entity Search");
                await AllureAttachResponseAsync(response, "Entity Search");
                throw new HttpRequestException(
                    $"Search Entity failed: {(int)response.StatusCode} {response.StatusCode}. Body: {raw}.");
            });
    }

    public async Task<EntityRecord> CreateAsync(string name)
    {
        return await AllureApi.Step($"Entity: Create entity '{name}'", async () =>
        {
            var body = new { entityName = name, activeFlag = true };
            var request = await CreateAuthorizedJsonRequestAsync(HttpMethod.Post, BasePath, body);
            var response = await Http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await AllureAttachRequestAsync(request, "Entity Create");
                await AllureAttachResponseAsync(response, "Entity Create");
                throw new HttpRequestException(
                    $"Create Entity failed: {(int)response.StatusCode} {response.StatusCode}. Body: {raw}");
            }

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

            var search = await SearchAsync(name, false, false, pageSize: 50, pageIndex: 0);
            var match = search.Records.FirstOrDefault(r =>
                string.Equals(r.EntityName, name, StringComparison.OrdinalIgnoreCase));

            if (match is null)
                throw new InvalidOperationException($"Entity '{name}' was created but not found via search.");

            return match;
        });
    }

    public async Task<EntityRecord> UpdateAsync(int entityId, object updateBody, string nameForFallback)
    {
        return await AllureApi.Step($"Entity: Update entity ID={entityId}", async () =>
        {
            var request = await CreateAuthorizedJsonRequestAsync(HttpMethod.Put, $"{BasePath}/{entityId}", updateBody);

            var response = await Http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                await AllureAttachRequestAsync(request, "Update Entity");
                await AllureAttachResponseAsync(response, "Update Entity");
                throw new HttpRequestException(
                    $"Update failed: {(int)response.StatusCode} {response.StatusCode}. Body: {raw}");
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

            var search = await SearchAsync(searchTerm: nameForFallback, showDeleted: null, showInactive: false,
                pageSize: 50);
            var match = search.Records.FirstOrDefault(c => c.EntityId == entityId &&
                                                           string.Equals(c.EntityName, nameForFallback,
                                                               StringComparison.OrdinalIgnoreCase));

            if (match is null)
                throw new InvalidOperationException("Entity was updated but not found via search.");

            return match;
        });
    }

    public Task<EntityRecord> UpdateAsync(int id, string name, string currentEmail)
    {
        var body = new
        {
            appUserId = 0,
            name,
            emailAddress = currentEmail,
            deleteFlag = false
        };

        return UpdateAsync(id, body, currentEmail);
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

    public async Task DeleteAsync(int id)
    {
        await AllureApi.Step($"Entity: Soft-delete entity ID={id}", async () =>
        {
            var request = await CreateAuthorizedJsonRequestAsync(HttpMethod.Delete, $"{BasePath}/{id}");

            var response = await Http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                await AllureAttachRequestAsync(request, "Entity Delete");
                await AllureAttachResponseAsync(response, "Entity Delete");
                throw new HttpRequestException($"Delete failed... Body: {raw}");
            }
        });
    }

    public async Task UndeleteAsync(int id)
    {
        await AllureApi.Step($"Entity: Undelete entity ID={id}", async () =>
        {
            var body = new { undelete = true };
            var request =
                await CreateAuthorizedJsonRequestAsync(HttpMethod.Delete, $"{BasePath}/{id}?undelete=true", body);

            var response = await Http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                await AllureAttachRequestAsync(request, "Entity Undelete");
                await AllureAttachResponseAsync(response, "Entity Undelete");
                throw new HttpRequestException($"Undelete failed... Body: {raw}");
            }
        });
    }
}