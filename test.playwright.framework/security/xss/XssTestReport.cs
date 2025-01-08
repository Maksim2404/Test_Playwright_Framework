namespace test.playwright.framework.security.xss;

public class XssTestReport
{
    public int TotalPayloadsTested { get; set; }
    public int TotalFieldsTested { get; set; }
    public int TotalPassed { get; set; }
    public int TotalFailed { get; set; }
    public List<XssTestDetails> TestDetails { get; set; } = [];
}