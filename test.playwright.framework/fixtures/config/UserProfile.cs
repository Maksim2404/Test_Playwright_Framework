using test.playwright.framework.pages.enums;

namespace test.playwright.framework.fixtures.config;

public sealed class UserProfile
{
    public required string Name { get; init; } //display name (first and last)
    public required string UserName { get; init; } //keycloak user email
    public required string Password { get; init; }
    public string? TotpSecret { get; init; } //null => no 2FA
    
    public UserNameKind Kind => Name.ParseKind();
}