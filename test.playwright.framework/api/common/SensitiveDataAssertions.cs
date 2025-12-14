using System.Text.RegularExpressions;
using FluentAssertions;

namespace test.playwright.framework.api.common;

public static partial class SensitiveDataAssertions
{
    [GeneratedRegex(@"eyJ[a-zA-Z0-9_\-]+?\.[a-zA-Z0-9_\-]+?\.[a-zA-Z0-9_\-]+", RegexOptions.Compiled)]
    private static partial Regex DetectTokenLongStrings();

    private static readonly Regex JwtLike = DetectTokenLongStrings();

    private static readonly string[] ForbiddenFragments =
    [
        "password", "passwd", "secret", "client_secret", "access_token", "refresh_token", "id_token", "authorization",
        "bearer ", "cookie", "set-cookie"
    ];

    public static void ShouldNotLeakSensitiveData(this string body, string context)
    {
        if (string.IsNullOrWhiteSpace(body)) return;

        var lower = body.ToLowerInvariant();

        foreach (var fragment in ForbiddenFragments)
        {
            lower.Should().NotContain(fragment, $"{context} should not include sensitive fragment '{fragment}'.");
        }

        JwtLike.IsMatch(body).Should().BeFalse($"{context} should not include JWT-like tokens.");
    }
}