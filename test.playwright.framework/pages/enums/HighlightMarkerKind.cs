namespace test.playwright.framework.pages.enums;

public enum HighlightMarkerKind
{
    GreenMarker,
    PinkMarker,
    BlueMarker,
    YellowMarker
}

internal static class HighlightMarkerExt
{
    private static readonly IReadOnlyDictionary<HighlightMarkerKind, string> Map =
        new Dictionary<HighlightMarkerKind, string>
        {
            [HighlightMarkerKind.GreenMarker] = "Green marker",
            [HighlightMarkerKind.PinkMarker] = "Pink marker",
            [HighlightMarkerKind.BlueMarker] = "Blue marker",
            [HighlightMarkerKind.YellowMarker] = "Yellow marker"
        };

    public static readonly IReadOnlyDictionary<HighlightMarkerKind, string> ColorToClassMap =
        new Dictionary<HighlightMarkerKind, string>
        {
            { HighlightMarkerKind.GreenMarker, "marker-green" },
            { HighlightMarkerKind.YellowMarker, "marker-yellow" },
            { HighlightMarkerKind.PinkMarker, "marker-pink" },
            { HighlightMarkerKind.BlueMarker, "marker-blue" }
        };

    public static string ToUi(this HighlightMarkerKind k) => Map[k];

    private static readonly IReadOnlyDictionary<string, HighlightMarkerKind> UiToKind =
        Map.ToDictionary(p => p.Value, p => p.Key, StringComparer.OrdinalIgnoreCase);

    public static HighlightMarkerKind ParseUi(this string uiText) => UiToKind.TryGetValue(uiText, out var state)
        ? state
        : throw new ArgumentException($"Unknown highlight-marker kind “{uiText}”.");
}