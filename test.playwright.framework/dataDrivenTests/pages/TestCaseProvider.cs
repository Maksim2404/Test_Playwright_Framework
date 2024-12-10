using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace test.playwright.framework.dataDrivenTests.pages;

public static class TestCaseProvider
{
    public static IEnumerable<TestCaseData> GetTestCases()
    {
        const string jsonPath = "TestCases.json";
        var jsonData = File.ReadAllText(jsonPath);
        var testCases = JsonConvert.DeserializeObject<dynamic>(jsonData)?.TestCases;

        if (testCases == null) yield break;
        foreach (var testCase in testCases)
        {
            yield return new TestCaseData(
                (string)testCase.NavigationPath,
                (string)testCase.TaskTitle,
                (string)testCase.Column,
                ((JArray)testCase.Tags).ToObject<string[]>()
            ).SetName((string)testCase.Name);
        }
    }
}