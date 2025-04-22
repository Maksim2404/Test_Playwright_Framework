using Bogus;

namespace test.playwright.framework.generators;

public static class TestDataGenerators
{
    private static readonly Faker Faker = new();
    private static readonly Random Random = new();
    
    private static readonly Dictionary<string, string[]> AnotherValueOfValues = new()
    {
        { "anotherValue1", ["value1"] },
        { "anotherValue2", ["value2"] },
    };
    
    private static readonly string[] Types =
    [
        "type1", "type2", "type3"
    ];

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

    public static string GenerateRandomFakerYear()
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
        var workType = new List<string> { "Type1", "Type2", "Type3", "Type4", "Type5", "Type6" };

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

    public static string GenerateRandomYear()
    {
        var currentYear = DateTime.Now.Year;
        var randomYear = Random.Next(2023, currentYear + 1);

        return randomYear.ToString();
    }

    private static void Shuffle<T>(IList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = Random.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
    
    public static string GenerateRandomValueBasedOnAnotherValue(string anotherValue)
    {
        if (AnotherValueOfValues.ContainsKey(anotherValue))
        {
            var titles = AnotherValueOfValues[anotherValue];
            return titles[Faker.Random.Int(0, titles.Length - 1)];
        }
        else
        {
            throw new ArgumentException("Customer not found in title mappings.", nameof(anotherValue));
        }
    }
    
    public static List<string> GenerateRandomMultipleTypes(int count, List<string>? excludeTypes = null)
    {
        excludeTypes ??= [];

        var availableTypes = Types.Except(excludeTypes).ToArray();
        if (count > availableTypes.Length)
        {
            throw new ArgumentException("The count is greater than the number of available types.");
        }

        var uniqueTypes = new HashSet<string>();
        while (uniqueTypes.Count < count)
        {
            var index = Random.Next(0, availableTypes.Length);
            uniqueTypes.Add(availableTypes[index]);
        }

        var shuffledTypes = uniqueTypes.ToList();
        Shuffle(shuffledTypes);
        return shuffledTypes;
    }
}