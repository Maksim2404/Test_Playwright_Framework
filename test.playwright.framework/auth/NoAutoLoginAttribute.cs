using NUnit.Framework;

namespace test.playwright.framework.auth;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class NoAutoLoginAttribute() : PropertyAttribute("NoAutoLogin");
