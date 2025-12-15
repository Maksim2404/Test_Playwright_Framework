using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using FluentAssertions;
using NUnit.Framework;
using Serilog;
using test.playwright.framework.api.apiClient;
using test.playwright.framework.api.@base;
using test.playwright.framework.api.common;
using test.playwright.framework.api.dto.entity_1;
using test.playwright.framework.api.testHelper.entity_1;

namespace test.playwright.framework.api.tests;

[TestFixture, AllureNUnit]
[AllureEpic("API")]
[AllureFeature("Settings")]
[AllureStory("Entity #1")]
[AllureOwner("QA")]
[AllureSuite("API - Settings")]
[AllureTag("Negative")]
public sealed class EntityNegativeTests : ApiTestBase
{
    private EntityApiClient _entities = null!;
    private EntityTestHelper _helper = null!;

    [SetUp]
    public void SetUp()
    {
        _entities = new EntityApiClient(Http, TokenProvider, ApiCfg);
        _helper = new EntityTestHelper(_entities);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _helper.DisposeAsync();
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(HttpRequestMessage request)
    {
        var token = await TokenProvider.GetAccessTokenAsync();
        token.Should().NotBeNullOrWhiteSpace("Admin token must be available for negative tests.");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await Http.SendAsync(request);
    }

    private Task<EntityRecord> CreateTestEntityAsync(string? suffix = null) => _helper.CreateTempEntityAsync(suffix);

    [Test]
    public async Task CreateEntity_WithoutAuth_Returns_Unauthorized()
    {
        var body = new { entityName = "NoAuthEntity" };
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/entity") { Content = JsonContent.Create(body) };

        var response = await Http.SendAsync(request);
        Assert.That(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected 401 or 403, got {(int)response.StatusCode} {response.StatusCode}");
    }

    [Test]
    public async Task CreateEntity_MissingName_Returns_BadRequest()
    {
        var token = await TokenProvider.GetAccessTokenAsync();
        token.Should().NotBeNullOrWhiteSpace("Token is null or empty");

        var body = new { entityName = "" };
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/entity") { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Http.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            $"Expected 400 BadRequest when missing entity name, but got {(int)response.StatusCode} {response.StatusCode}.");

        var errorBody = await response.Content.ReadAsStringAsync();
        Log.Information("Validation response: {Body}", errorBody);
    }

    [Test]
    public async Task CreatingEntity_WithDuplicateName_ReturnsBadRequest()
    {
        var first = await CreateTestEntityAsync("DuplicateBase");
        first.ShouldHaveValidId();
        first.ShouldBeActiveAndNotDeleted();

        var duplicateBody = new { entityName = first.EntityName, activeFlag = true };
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/entity")
            { Content = JsonContent.Create(duplicateBody) };

        var response = await SendAuthorizedAsync(request);
        var error = await response.ShouldBeApiErrorAsync(HttpStatusCode.BadRequest);
        error.ShouldHaveCustomMessageContaining("already exists");
    }

    [Test]
    public async Task CreatingEntity_WithTooLongName_ReturnsValidationError()
    {
        var tooLongName = new string('A', 50);

        var body = new { entityName = tooLongName, activeFlag = true };
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/entity") { Content = JsonContent.Create(body) };

        var response = await SendAuthorizedAsync(request);
        var error = await response.ShouldBeApiErrorAsync(HttpStatusCode.BadRequest);
        error.ShouldHaveValidationErrorFor(fieldName: "EntityName", expectedMessageFragment: "maximum length");
    }

    [Test, Category("Security")]
    public async Task Entity_AllowsScriptLikeText_InName_IsStoredAsData()
    {
        var now = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var name = $"API_Entity_<script>{now}</script>";

        try
        {
            var created = await _entities.CreateAsync(name);
            created.EntityName.Should().Be(name);

            var found = await _entities.FindByNameAsync(name);
            found.Should().NotBeNull();
            found!.EntityName.Should().Be(name);
        }
        catch (HttpRequestException)
        {
            Assert.Pass("API rejected script-like name.");
        }
    }
}