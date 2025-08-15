namespace test.playwright.framework.utils.interfaces;

public interface IVideoCopier
{
    void CopyLastVideo(string sourceDir, string destDir, string testName);
}
