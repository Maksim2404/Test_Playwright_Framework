using NUnit.Framework;
using test.playwright.framework.dataDrivenTests.pages;

namespace test.playwright.framework.dataDrivenTests.tests;

[TestFixture]
public class DataDrivenTests : DataDrivenBaseTest
{
    private LoginPage _loginPage;
    private MainPage _mainPage;

    [SetUp]
    public async Task SetUpTestAsync()
    {
        const string userName = "admin";
        const string password = "password123";

        _loginPage = new LoginPage(Page);
        _mainPage = new MainPage(Page);

        await _loginPage.LoginAsync(userName, password);
    }

    [Test]
    [TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetTestCases))]
    public async Task VerifyTask(string navigationPath, string taskTitle, string columnName, string[] tags)
    {
        await _mainPage.NavigateToSectionAsync(navigationPath);

        var taskVerified = await _mainPage.VerifyTaskInColumnAsync(taskTitle, columnName, tags);
        Assert.IsTrue(taskVerified,
            $"Task '{taskTitle}' verification failed in column '{columnName}' with tags '{string.Join(", ", tags)}'.");
    }
}