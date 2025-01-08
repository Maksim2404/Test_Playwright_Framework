namespace test.playwright.framework.security.sql;

public class SqlInjectionTestReport
{
    public int TotalPayloadsTested { get; set; }
    public int TotalFieldsTested { get; set; }
    public int TotalPassed { get; set; }
    public int TotalFailed { get; set; }
    public List<SqlInjectionTestDetails> TestDetails { get; set; } = new();
}