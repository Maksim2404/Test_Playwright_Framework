namespace test.playwright.framework.pages.enums;

public enum UserNameKind
{
    Unassigned = 0,
    Admin,
    Public
}

internal static class UserCatalog
{
    private static readonly IReadOnlyDictionary<UserNameKind, string> Name =
        new Dictionary<UserNameKind, string>
        {
            [UserNameKind.Unassigned] = "Unassigned",
            [UserNameKind.Admin] = "QA Admin",
            [UserNameKind.Public] = "QA Public"
        };

    public static readonly Dictionary<UserNameKind, string[]> Email = new()
    {
        { UserNameKind.Admin, ["admin@example.com"] },
        { UserNameKind.Public, ["public@example.com"] }
    };

    private static readonly IReadOnlyDictionary<string, UserNameKind> UiToKind =
        Name.ToDictionary(p => p.Value, p => p.Key, StringComparer.OrdinalIgnoreCase);

    public static string ToUi(this UserNameKind k) => Name[k];

    public static UserNameKind ParseKind(this string uiText) =>
        UiToKind.GetValueOrDefault(uiText, UserNameKind.Unassigned);
}