namespace test.playwright.framework.security.sql;

public class SqlInjectionTestDetails
{
    public string FieldName { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public bool IsSanitized { get; set; }
    public string Notes { get; set; } = string.Empty;
}