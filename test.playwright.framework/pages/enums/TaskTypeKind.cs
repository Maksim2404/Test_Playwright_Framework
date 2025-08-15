namespace test.playwright.framework.pages.enums;

public enum TaskTypeKind
{
    TypeOne,
    TypeTwo,
}

internal static class TaskTypeKindExtensions
{
    private static readonly IReadOnlyDictionary<TaskTypeKind, string> Map =
        new Dictionary<TaskTypeKind, string>
        {
            [TaskTypeKind.TypeOne] = "TypeOne",
            [TaskTypeKind.TypeTwo] = "TypeTwo"
        };

    private static readonly IReadOnlyDictionary<string, TaskTypeKind> UiToKind =
        Map.ToDictionary(p => p.Value, p => p.Key, StringComparer.OrdinalIgnoreCase);

    public static string ToUi(this TaskTypeKind s) => Map[s];

    public static TaskTypeKind ParseUi(this string uiText) => UiToKind.TryGetValue(uiText, out var state)
        ? state
        : throw new ArgumentException($"Unknown task-type-kind label “{uiText}”.");
}
