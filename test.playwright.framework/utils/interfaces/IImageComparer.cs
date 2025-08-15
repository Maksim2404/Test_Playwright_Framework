namespace test.playwright.framework.utils.interfaces;

public interface IImageComparer
{
    Task<bool> AreEqualAsync(string baseline, string actual, string diffPath);
}
