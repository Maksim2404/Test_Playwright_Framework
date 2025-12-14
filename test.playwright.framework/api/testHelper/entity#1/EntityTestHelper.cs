using test.playwright.framework.api.apiClient;
using test.playwright.framework.api.dto.entity_1;

namespace test.playwright.framework.api.testHelper.entity_1;

public sealed class EntityTestHelper(EntityApiClient entities) : IAsyncDisposable
{
    private readonly List<int> _createdEntityIds = [];
    
    private void Register(EntityRecord record)
    {
        if (!_createdEntityIds.Contains(record.EntityId)) _createdEntityIds.Add(record.EntityId);
    }

    public async Task<EntityRecord> CreateTempEntityAsync(string? nameSuffix = null)
    {
        var nowPart = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var labelPart = string.IsNullOrWhiteSpace(nameSuffix) ? "" : $"_{nameSuffix}";
        var name = $"API_Entity_{nowPart}{labelPart}";

        var created = await entities.CreateAsync(name);

        Register(created);
        return created;
    }

    public async Task<(IReadOnlyList<EntityRecord> Records, string Prefix)> CreateBatchAsync(string logicalLabel,
        int count = 5)
    {
        var records = new List<EntityRecord>();
        var prefix = $"API_{logicalLabel}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_";

        for (var i = 0; i < count; i++)
        {
            var name = $"{prefix}{i}";
            var record = await entities.CreateAsync(name);

            Register(record);
            records.Add(record);
        }

        return (records, prefix);
    }

    private async Task CleanupAsync()
    {
        if (_createdEntityIds.Count == 0) return;

        foreach (var id in _createdEntityIds.ToList())
        {
            try
            {
                await entities.DeleteAsync(id);
            }
            catch
            {
                // record might already be soft-deleted or not found.
            }
        }

        _createdEntityIds.Clear();
    }

    public async ValueTask DisposeAsync() => await CleanupAsync();
}