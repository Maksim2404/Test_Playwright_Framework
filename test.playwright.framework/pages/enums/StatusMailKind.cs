namespace test.playwright.framework.pages.enums;

public enum StatusMailKind
{
    Ready
}

internal static class StatusMailKindExt
{
    public static string Subject(this StatusMailKind k, string industry, string category) => k switch
    {
        StatusMailKind.Ready => $"Item Ready | {industry} | {category}",
        _ => throw new ArgumentOutOfRangeException(nameof(k), k, null)
    };

    public static string BodyTemplate(this StatusMailKind k, string user) => k switch
    {
        StatusMailKind.Ready => $"Item status has changed to Ready by {user} .",
        _ => throw new ArgumentOutOfRangeException(nameof(k), k, null)
    };

    public static string BodyLocatorCss(this StatusMailKind k) => k switch
    {
        StatusMailKind.Ready
            => "tbody tr td h2:nth-child(1)",

        /*// resolved uses success alert styling
        StatusMailKind.Ready => "table.alert-success tbody tr td",*/

        _ => throw new ArgumentOutOfRangeException(nameof(k), k, null)
    };
}