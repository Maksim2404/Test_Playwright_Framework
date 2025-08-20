namespace test.playwright.framework.pages.enums;

public enum CustomerKind
{
    CustomerOne
}

internal static class CustomerCatalog
{
    private static readonly IReadOnlyDictionary<CustomerKind, string> Map =
        new Dictionary<CustomerKind, string>
        {
            [CustomerKind.CustomerOne] = "FREELANCE"
        };

    private static readonly IReadOnlyDictionary<string, CustomerKind> UiToKind =
        Map.ToDictionary(p => p.Value, p => p.Key, StringComparer.OrdinalIgnoreCase);

    public static string ToUi(this CustomerKind k) => Map.TryGetValue(k, out var ui)
        ? ui
        : throw new InvalidOperationException($"CustomerKind {k} has no UI label.");

    public static CustomerKind ParseUi(this string uiText) => UiToKind.TryGetValue(uiText, out var kind)
        ? kind
        : throw new ArgumentException($"Unknown customer label “{uiText}”.");
}