namespace test.playwright.framework.security.xss;

public class XssTestDetails
{
    public string FieldName { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public bool IsSanitized { get; set; }
    public bool NoAlertTriggered { get; set; }
    public bool TestPassed => IsSanitized && NoAlertTriggered;
}