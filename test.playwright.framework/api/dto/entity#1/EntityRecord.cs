using System.Text.Json.Serialization;

namespace test.playwright.framework.api.dto.entity_1;

public sealed class EntityRecord
{
    [JsonPropertyName("entityId")] public int EntityId { get; init; }
    [JsonPropertyName("entityName")] public string EntityName { get; init; } = null!;
    [JsonPropertyName("activeFlag")] public bool ActiveFlag { get; init; }
    [JsonPropertyName("deleteFlag")] public bool DeleteFlag { get; init; }
}