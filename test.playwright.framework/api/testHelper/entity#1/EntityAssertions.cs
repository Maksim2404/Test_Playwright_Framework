using FluentAssertions;
using test.playwright.framework.api.dto.entity_1;

namespace test.playwright.framework.api.testHelper.entity_1;

public static class EntityAssertions
{
    public static void ShouldBeInactiveAndNotDeleted(this EntityRecord record)
    {
        record.ActiveFlag.Should().BeFalse("Expected ActiveFlag=false (entity should be inactive).");
        record.DeleteFlag.Should().BeFalse("Expected DeleteFlag=false (entity should not be deleted).");
    }

    public static void ShouldBeActiveAndNotDeleted(this EntityRecord record)
    {
        record.ActiveFlag.Should().BeTrue("Expected ActiveFlag=true (entity should be active).");
        record.DeleteFlag.Should().BeFalse("Expected DeleteFlag=false (entity should not be deleted).");
    }

    public static void ShouldBeSoftDeleted(this EntityRecord record) => record.DeleteFlag
        .Should().BeTrue("Expected DeleteFlag=true (entity should be deleted).");

    public static void ShouldBeUndeleted(this EntityRecord record) => record.DeleteFlag
        .Should().BeFalse("Expected DeleteFlag=false (entity should be undeleted).");

    public static void ShouldMatchName(this EntityRecord record, string expectedName) => record.EntityName
        .Should().Be(expectedName, "Expected EntityName to match.");

    public static void ShouldHaveValidId(this EntityRecord record) =>
        record.EntityId.Should().BeGreaterThan(0, "Expected EntityId to be greater than 0.");
}