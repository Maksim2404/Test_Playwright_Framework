using NUnit.Framework;

namespace test.playwright.framework.fixtures.auth;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class NoAutoLoginAttribute() : PropertyAttribute("NoAutoLogin");
