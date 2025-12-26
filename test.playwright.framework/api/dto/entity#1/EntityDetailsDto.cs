using System.Text.Json.Serialization;

namespace test.playwright.framework.api.dto.entity_1;

public sealed class EntityDetailsDto
{
    [JsonPropertyName("appUserId")] public int AppUserId { get; init; }
    [JsonPropertyName("deleteFlag")] public bool DeleteFlag { get; init; }
    [JsonPropertyName("emailAddress")] public string EmailAddress { get; init; } = null!;
    [JsonPropertyName("skills")] public List<SkillsDto>? Skills { get; init; } = [];
    [JsonPropertyName("createdDate")] public DateTimeOffset? CreatedDate { get; init; }
}

public sealed class SkillsDto
{
    [JsonPropertyName("skillId")] public int SkillId { get; init; }
    [JsonPropertyName("name")] public string SkillName { get; init; } = null!;
}