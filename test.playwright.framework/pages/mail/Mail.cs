using test.playwright.framework.pages.enums;

namespace test.playwright.framework.pages.mail;

public static class Mail
{
    public static MailSpec Status(StatusMailKind kind, string industry, string category, string? user = null) =>
        new(
            Subject: kind.Subject(industry, category),
            BodyCss: kind.BodyLocatorCss(),
            ExpectedBody: user is null ? null : kind.BodyTemplate(user)
        );
}