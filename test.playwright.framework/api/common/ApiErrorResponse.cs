using System.Text.Json.Serialization;

namespace test.playwright.framework.api.common;

public sealed class ApiErrorResponse
{
    [JsonPropertyName("title")] public string? Title { get; init; }
    [JsonPropertyName("status")] public int? Status { get; init; }
    [JsonPropertyName("detail")] public string? Detail { get; init; }
    [JsonPropertyName("errors")] public Dictionary<string, string[]>? Errors { get; init; }
    [JsonPropertyName("errorType")] public string? ErrorType { get; init; }
    [JsonPropertyName("errorMessage")] public string? ErrorMessage { get; init; }
}