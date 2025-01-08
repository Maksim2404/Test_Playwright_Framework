namespace test.playwright.framework.security.xss;

public static class XssPayloads
{
    public static readonly List<string> BasicPayloads =
    [
        "<script>alert('XSS')</script>",
        "<img src=x onerror=alert('XSS')>",
        "<svg/onload=alert('XSS')>"
    ];

    public static readonly List<string> ObfuscatedPayloads =
    [
        "<scr<script>ipt>alert('XSS')</scr<script>ipt>",
        "<svg><s<t<script>cript>alert(1)</s<t<script>cript>",
        "<a href='javascript:alert(\"XSS\")'>Click me</a>"
    ];

    public static readonly List<string> EncodedPayloads =
    [
        "%3Cscript%3Ealert('XSS')%3C%2Fscript%3E",
        "%3Cimg%20src%3Dx%20onerror%3Dalert('XSS')%3E",
        "%3Csvg%2Fonload%3Dalert('XSS')%3E"
    ];
}