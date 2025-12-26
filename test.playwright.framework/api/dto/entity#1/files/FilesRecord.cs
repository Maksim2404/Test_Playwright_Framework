using System.Text.Json.Serialization;

namespace test.playwright.framework.api.dto.entity_1.files;

public sealed class FilesRecord
{
    [JsonPropertyName("fileId")] public int FileId { get; init; }
    [JsonPropertyName("fileName")] public string FileName { get; init; } = null!;
    [JsonPropertyName("contentType")] public string ContentType { get; init; } = null!;
    [JsonPropertyName("length")] public long Length { get; init; }
    [JsonPropertyName("fileType")] public FileTypeDto FileType { get; init; } = null!;
    [JsonPropertyName("deleteFlag")] public bool DeleteFlag { get; init; }
    [JsonPropertyName("activeFlag")] public bool ActiveFlag { get; init; }
    [JsonPropertyName("createdDate")] public DateTimeOffset CreatedDate { get; init; }
}

public sealed class FileTypeDto
{
    [JsonPropertyName("fileTypeId")] public int FileTypeId { get; init; }
    [JsonPropertyName("fileTypeName")] public string FileTypeName { get; init; } = null!;
    [JsonPropertyName("activeFlag")] public bool ActiveFlag { get; init; }
    [JsonPropertyName("deleteFlag")] public bool DeleteFlag { get; init; }
}