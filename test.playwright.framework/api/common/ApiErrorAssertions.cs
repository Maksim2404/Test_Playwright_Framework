using System.Net;
using System.Text.Json;
using FluentAssertions;
using Serilog;

namespace test.playwright.framework.api.common;

public static class ApiErrorAssertions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static void ShouldHaveCustomMessageContaining(this ApiErrorResponse error, string fragment)
    {
        error.ErrorMessage.Should()
            .NotBeNullOrWhiteSpace("Expected a business error message (errorMessage) in the response.");

        error.ErrorMessage!.Should().Contain(fragment);
    }

    public static async Task<ApiErrorResponse> ShouldBeApiErrorAsync(this HttpResponseMessage response,
        HttpStatusCode expectedStatus)
    {
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(expectedStatus, $"expected HTTP {(int)expectedStatus} {expectedStatus} " +
                                                        $"but got {(int)response.StatusCode} {response.StatusCode}");

        var body = await response.Content.ReadAsStringAsync();
        Log.Information("API error raw body: {Body}", body);
        body.Should().NotBeNull("API error response body should not be null.");
        body.ShouldNotLeakSensitiveData($"Error response body for {(int)expectedStatus} {expectedStatus}");

        ApiErrorResponse? error;
        try
        {
            error = JsonSerializer.Deserialize<ApiErrorResponse>(body, JsonOptions);

            (string.IsNullOrWhiteSpace(error!.Title) && string.IsNullOrWhiteSpace(error.Detail) &&
             (error.Errors is null || error.Errors.Count == 0) && string.IsNullOrWhiteSpace(error.ErrorMessage))
                .Should().BeFalse("Error payload should contain at least one meaningful error field.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize API error response into ApiErrorResponse. Raw body: {body}", ex);
        }

        error.Should().NotBeNull("API error payload should match ApiErrorResponse shape.");
        if (error.Status is not null)
            error.Status.Should().Be((int)expectedStatus, "error status (when present) should match HTTP status code.");

        return error;
    }

    public static void ShouldHaveValidationErrorFor(this ApiErrorResponse error, string fieldName,
        string? expectedMessageFragment = null)
    {
        error.Errors.Should().NotBeNull("Validation errors dictionary should be present for validation failures.");
        error.Errors.Should().ContainKey(fieldName, $"Validation errors should contain a key for '{fieldName}'.");

        var messages = error.Errors?[fieldName];
        messages.Should().NotBeNullOrEmpty($"There should be at least one validation message for '{fieldName}'.");

        if (!string.IsNullOrWhiteSpace(expectedMessageFragment))
        {
            (messages != null &&
             messages.Any(m => m.Contains(expectedMessageFragment, StringComparison.OrdinalIgnoreCase)))
                .Should().BeTrue(
                    $"Expected at least one validation message for '{fieldName}' to contain '{expectedMessageFragment}'.");
        }
    }
}