using System.Text.Json.Serialization;

namespace test.playwright.framework.api.dto.entity_1.files;

public sealed class FilesSearchResponse
{
    [JsonPropertyName("totalRecords")] public int TotalRecords { get; init; }
    [JsonPropertyName("records")] public List<FilesRecord> Records { get; init; } = [];
}