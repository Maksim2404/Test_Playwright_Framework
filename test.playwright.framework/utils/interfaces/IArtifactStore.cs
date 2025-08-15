namespace test.playwright.framework.utils.interfaces;

public interface IArtifactStore
{
    Task<string> SaveScreenshotAsync(byte[] buffer, string name);
    void AttachToReport(byte[] buffer, string title);
}
