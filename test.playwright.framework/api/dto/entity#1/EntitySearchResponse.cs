using System.Text.Json.Serialization;

namespace test.playwright.framework.api.dto.entity_1;

public sealed class EntitySearchResponse
{
    [JsonPropertyName("totalRecords")] public int TotalRecords { get; init; }
    [JsonPropertyName("records")] public List<EntityRecord> Records { get; init; } = [];
}