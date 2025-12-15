using Allure.NUnit;
using Allure.NUnit.Attributes;
using NUnit.Framework;
using test.playwright.framework.api.apiClient;
using test.playwright.framework.api.@base;
using test.playwright.framework.api.testHelper.entity_1;

namespace test.playwright.framework.api.tests;

[TestFixture, AllureNUnit]
[AllureEpic("API")]
[AllureFeature("Settings")]
[AllureStory("Entity #1")]
[AllureOwner("QA")]
[AllureSuite("API - Settings")]
[AllureTag("Pagination")]
public sealed class EntityPaginationTests : ApiTestBase
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

    [Test]
    public async Task EntitySearch_PaginatesConsistently_ForMatchingPrefix()
    {
        var (created, prefix) = await _helper.CreateBatchAsync("PageTest");
        var createdIds = created.Select(l => l.EntityId).ToList();
        const int pageSize = 2;
        

        var page0 = await _entities.SearchAsync(searchTerm: prefix, showDeleted: null,
            showInactive: false, pageSize: pageSize, pageIndex: 0);

        var page1 = await _entities.SearchAsync(searchTerm: prefix, showDeleted: null,
            showInactive: false, pageSize: pageSize, pageIndex: 1);

        var page2 = await _entities.SearchAsync(searchTerm: prefix, showDeleted: null,
            showInactive: false, pageSize: pageSize, pageIndex: 2);

        Assert.That(page0.TotalRecords, Is.EqualTo(createdIds.Count),
            "totalRecords should match the number of created entities for this prefix.");

        Assert.That(page0.Records.Count, Is.EqualTo(2), "First page should have 2 records.");
        Assert.That(page1.Records.Count, Is.EqualTo(2), "Second page should have 2 records.");
        Assert.That(page2.Records.Count, Is.EqualTo(1), "Third page should have 1 record.");
    }

    [Test]
    public async Task EntitySearch_Pagination_HasNoDuplicates()
    {
        var (created, prefix) = await _helper.CreateBatchAsync("PageTest");
        var createdIds = created.Select(l => l.EntityId).ToList();
        const int pageSize = 2;

        var page0 = await _entities.SearchAsync(searchTerm: prefix, showDeleted: null,
            showInactive: false, pageSize: pageSize, pageIndex: 0);

        var page1 = await _entities.SearchAsync(searchTerm: prefix, showDeleted: null,
            showInactive: false, pageSize: pageSize, pageIndex: 1);

        var page2 = await _entities.SearchAsync(searchTerm: prefix, showDeleted: null,
            showInactive: false, pageSize: pageSize, pageIndex: 2);

        var pagedIds = page0.Records.Concat(page1.Records).Concat(page2.Records).Select(lr => lr.EntityId).ToList();

        Assert.That(pagedIds.Distinct().Count(), Is.EqualTo(pagedIds.Count),
            "Pagination should not contain duplicate records across pages.");

        Assert.That(pagedIds, Is.EquivalentTo(createdIds),
            "Union of paged results should exactly match the set of created entities.");
    }
}