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
[AllureTag("CRUD")]
public sealed class EntityCrudTests : ApiTestBase
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

    [Test, Category("Smoke")]
    [AllureTag("Smoke")]
    public async Task EntitySearch_Returns_Expected_Page()
    {
        var dto = await _entities.SearchAsync(searchTerm: null, false, false);

        dto.Should().NotBeNull();
        dto.Records.Count.Should().BeInRange(0, 20);
    }

    [Test]
    public async Task NewEntity_IsActiveByDefault_AndVisible()
    {
        var created = await CreateTestEntityAsync("CreateActive");
        created.ShouldHaveValidId();
        created.ShouldBeActiveAndNotDeleted();

        var found = await _entities.FindByNameAsync(created.EntityName);
        found.Should().NotBeNull("Active entity not found when Show Inactive = OFF");
        found!.ShouldBeActiveAndNotDeleted();
    }

    [Test]
    public async Task Entity_CanBeUpdated()
    {
        var created = await CreateTestEntityAsync("CreateActive");
        created.ShouldHaveValidId();
        created.ShouldBeActiveAndNotDeleted();

        var originalId = created.EntityId;
        var originalName = created.EntityName;

        var newName = originalName + "_UPDATED";
        var updated = await _entities.UpdateAsync(entityId: originalId, name: newName, activeFlag: true);

        updated.EntityId.Should().Be(originalId, "update should not change the entity's ID.");
        updated.ShouldMatchName(newName);
        updated.ShouldBeActiveAndNotDeleted();

        var viaSearch = await _entities.FindByNameAsync(newName, includeInactive: false, showDeleted: null);
        viaSearch.Should().NotBeNull("Updated entity should be found via search by new name.");
        viaSearch!.EntityId.Should().Be(originalId);
        viaSearch.ShouldMatchName(newName);
        viaSearch.ShouldBeActiveAndNotDeleted();
    }

    [Test]
    public async Task Entity_CanBeDeactivated()
    {
        var created = await CreateTestEntityAsync("Deactivate");
        created.ShouldHaveValidId();
        created.ShouldBeActiveAndNotDeleted();

        await _entities.SetActiveAsync(created, active: false);

        var found = await _entities.FindByNameAsync(created.EntityName, includeInactive: true, showDeleted: null);
        found.Should().NotBeNull("Entity should still be present when including inactive records.");
        found!.ShouldBeInactiveAndNotDeleted();
    }

    [Test]
    public async Task Entity_CanBeReactivated()
    {
        var created = await CreateTestEntityAsync("Reactivate");
        created.ShouldHaveValidId();
        created.ShouldBeActiveAndNotDeleted();

        await _entities.SetActiveAsync(created, active: false);

        var inactive = await _entities.FindByNameAsync(created.EntityName, includeInactive: true, showDeleted: null);
        inactive.Should().NotBeNull();
        inactive!.ShouldBeInactiveAndNotDeleted();

        await _entities.SetActiveAsync(inactive!, active: true);

        var reactivated =
            await _entities.FindByNameAsync(created.EntityName, includeInactive: false, showDeleted: null);
        reactivated.Should().NotBeNull("Reactivated entity should be visible in normal view.");
        reactivated!.ShouldBeActiveAndNotDeleted();
    }
    
    [Test]
    public async Task Entity_CanBeSoftDeleted()
    {
        var created = await CreateTestEntityAsync("SoftDelete");
        created.ShouldHaveValidId();
        created.ShouldBeActiveAndNotDeleted();

        await _entities.DeleteAsync(created.EntityId);

        var normalView =
            await _entities.FindByNameAsync(created.EntityName, includeInactive: false, showDeleted: null);
        normalView.Should().BeNull("Soft-deleted entity should not appear in non-deleted view.");

        var deletedView =
            await _entities.FindByNameAsync(created.EntityName, includeInactive: false, showDeleted: true);
        deletedView.Should().NotBeNull("Soft-deleted entity should appear when showDeleted=true.");
        deletedView!.ShouldBeSoftDeleted();
    }

    [Test]
    public async Task Entity_CanBeUndeleted()
    {
        var created = await CreateTestEntityAsync("Undelete");
        created.ShouldHaveValidId();
        created.ShouldBeActiveAndNotDeleted();

        await _entities.DeleteAsync(created.EntityId);

        var deletedView =
            await _entities.FindByNameAsync(created.EntityName, includeInactive: false, showDeleted: true);
        deletedView.Should().NotBeNull("Soft-deleted entity should appear in deleted view before undelete.");
        deletedView!.ShouldBeSoftDeleted();

        await _entities.UndeleteAsync(created.EntityId);

        var deletedAfterUndelete =
            await _entities.FindByNameAsync(created.EntityName, includeInactive: false, showDeleted: true);
        deletedAfterUndelete.Should().BeNull("Entity should not appear in deleted view after undelete.");

        var normalView =
            await _entities.FindByNameAsync(created.EntityName, includeInactive: false, showDeleted: null);
        normalView.Should().NotBeNull("Entity should be visible again after undelete.");
        normalView!.ShouldBeActiveAndNotDeleted();
    }
}