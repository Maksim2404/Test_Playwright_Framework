using test.playwright.framework.config;

namespace test.playwright.framework.auth;

public class Contracts
{
    /// <summary>How the user should authenticate.</summary>
    public enum LoginMode
    {
        PasswordOnly,
        Totp
    }

    /// <summary>Packaged request sent to AuthManager.</summary>
    public sealed record LoginRequest(UserProfile Profile, LoginMode Mode);

    /// <summary>Abstraction so we can swap where profiles come from (secrets, vault, CI).</summary>
    public interface IProfileProvider
    {
        UserProfile GetByName(string logicalName);
    }
}