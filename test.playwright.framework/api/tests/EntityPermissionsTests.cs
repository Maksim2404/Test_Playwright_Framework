using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using FluentAssertions;
using NUnit.Framework;
using test.playwright.framework.api.apiClient;
using test.playwright.framework.api.@base;
using test.playwright.framework.api.dto.entity_1;
using test.playwright.framework.api.testHelper.entity_1;

namespace test.playwright.framework.api.tests;

[TestFixture, AllureNUnit]
[AllureEpic("API")]
[AllureFeature("Settings")]
[AllureStory("Entity #1")]
[AllureOwner("QA")]
[AllureSuite("API - Settings")]
[AllureTag("Permissions")]
public sealed class EntityPermissionsTests : ApiTestBase
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

    private Task<EntityRecord> CreateTestEntityAsync(string? suffix = null) => _helper.CreateTempEntityAsync(suffix);

    [Test]
    public async Task PublicUser_CannotCreateEntity()
    {
        if (string.IsNullOrWhiteSpace(ApiCfg.PublicUsername) || string.IsNullOrWhiteSpace(ApiCfg.PublicPassword))
            Assert.Ignore("Public (non-admin) user credentials are not configured in ApiConfig.");

        var token = await TokenProvider.GetAccessTokenAsync(ApiCfg.PublicUsername!, ApiCfg.PublicPassword!);
        token.Should().NotBeNullOrWhiteSpace("Public user Token is null or empty.");

        var body = new { entityName = "PublicUserEntity_Attempt" };
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/entity") { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Http.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            $"Expected 401 or 403 for public user, but got {(int)response.StatusCode} {response.StatusCode}.");
    }

    [Test]
    public async Task PublicUser_CannotUpdateEntity()
    {
        if (string.IsNullOrWhiteSpace(ApiCfg.PublicUsername) || string.IsNullOrWhiteSpace(ApiCfg.PublicPassword))
            Assert.Ignore("Public (non-admin) user credentials are not configured in ApiConfig.");

        var created = await CreateTestEntityAsync();
        var originalName = created.EntityName;
        created.ShouldHaveValidId();
        created.ShouldBeActiveAndNotDeleted();

        var publicToken = await TokenProvider.GetAccessTokenAsync(ApiCfg.PublicUsername!, ApiCfg.PublicPassword!);
        publicToken.Should().NotBeNullOrWhiteSpace("Public user Token is null or empty.");

        var maliciousBody = new { entityName = originalName + "_HACKED", activeFlag = true };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/entity/{created.EntityId}")
            { Content = JsonContent.Create(maliciousBody) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", publicToken);

        var response = await Http.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            $"Expected 401 or 403 for public user, but got {(int)response.StatusCode} {response.StatusCode}.");

        var after = await _entities.FindByNameAsync(originalName);
        after.Should().NotBeNull("Entity not found after failed public update attempt.");
        after!.ShouldMatchName(originalName);
        after!.ShouldBeActiveAndNotDeleted();
    }

    [Test]
    public async Task PublicUser_CannotDeleteEntity()
    {
        if (string.IsNullOrWhiteSpace(ApiCfg.PublicUsername) || string.IsNullOrWhiteSpace(ApiCfg.PublicPassword))
            Assert.Ignore("Public (non-admin) user credentials are not configured in ApiConfig.");

        var created = await CreateTestEntityAsync();
        created.ShouldHaveValidId();
        created.ShouldBeActiveAndNotDeleted();

        var before = await _entities.FindByNameAsync(created.EntityName);
        before.Should().NotBeNull("Language not found before delete attempt.");
        before!.ShouldBeUndeleted();

        var publicToken = await TokenProvider.GetAccessTokenAsync(ApiCfg.PublicUsername!, ApiCfg.PublicPassword!);
        publicToken.Should().NotBeNullOrWhiteSpace("Public user Token is null or empty.");

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/entity/{created.EntityId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", publicToken);

        var response = await Http.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            $"Expected 401 or 403 for public user, but got {(int)response.StatusCode} {response.StatusCode}.");

        var after = await _entities.FindByNameAsync(created.EntityName);
        after.Should().NotBeNull("Entity not found after failed public delete attempt.");
        after!.ShouldBeUndeleted();
    }
}