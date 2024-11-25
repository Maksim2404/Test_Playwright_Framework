using Bogus;

namespace test.playwright.framework.generators;

public static class TestDataGenerators
{
    private static readonly Faker Faker = new();
    private static readonly Random Random = new();

    public static string GenerateRandomText(int length)
    {
        return Faker.Random.String2(length);
    }

    public static string GenerateRandomNumber(int digits)
    {
        var minValue = (int)Math.Pow(10, digits - 1);
        var maxValue = (int)Math.Pow(10, digits) - 1;
        return Faker.Random.Number(minValue, maxValue).ToString();
    }

    public static string GenerateRandomFileName()
    {
        return Faker.System.FileName();
    }

    public static string GenerateRandomYear()
    {
        return Faker.Date.Future().Year.ToString();
    }

    public static string GenerateRandomEmail()
    {
        const string domain = "test.com";

        var userName = Faker.System.FileName().Replace(".", "").Substring(0, 8);
        var email = $"{userName}@{domain}";

        return email;
    }

    public static string GenerateRandomWorkType(string? excludeWorkType = null)
    {
        var workType = new List<string>
            { "Features", "Trailers", "Streaming", "Localization", "Roadshow", "Downstream" };

        if (!string.IsNullOrEmpty(excludeWorkType))
        {
            workType.Remove(excludeWorkType);
        }

        return workType[Faker.Random.Int(0, workType.Count - 1)];
    }

    public static string GenerateRandomTime()
    {
        var hours = Random.Next(0, 24);
        var minutes = Random.Next(0, 60);

        var time = $"{hours:D2}:{minutes:D2}";
        return time;
    }
}