using Bogus;
using test.playwright.framework.pages.enums;

namespace test.playwright.framework.generators;

public static class TestDataGenerators
{
    private static readonly Faker Faker = new();
    private static readonly Random Random = new();

    private static bool DefaultIsSentinel<T>() where T : struct, Enum
    {
        return Enum.TryParse("None", ignoreCase: true, out T sentinel) &&
               EqualityComparer<T>.Default.Equals(default, sentinel);
    }

    public static T RandomEnum<T>(params T[] exclude) where T : struct, Enum
    {
        var exclusions = new HashSet<T>(exclude);
        if (DefaultIsSentinel<T>()) exclusions.Add(default);

        var pool = Enum.GetValues<T>().Except(exclusions).ToArray();
        return Faker.PickRandom(pool);
    }

    public static List<T> RandomEnumMany<T>(int count, IEnumerable<T>? exclude = null) where T : struct, Enum
    {
        var exclusions = new HashSet<T>(exclude ?? []);
        if (DefaultIsSentinel<T>()) exclusions.Add(default);

        var pool = Enum.GetValues<T>().Except(exclusions).ToList();
        if (count > pool.Count)
            throw new ArgumentException($"Requested {count} items but only {pool.Count} available.");

        return Faker.Random.ListItems(pool, count).ToList();
    }

    public static string RandomText(int length)
    {
        return Faker.Random.String2(length);
    }

    public static string RandomNumber(int digits)
    {
        var minValue = (int)Math.Pow(10, digits - 1);
        var maxValue = (int)Math.Pow(10, digits) - 1;
        return Faker.Random.Number(minValue, maxValue).ToString();
    }

    public static string RandomFileName()
    {
        return Faker.System.FileName();
    }

    public static string RandomYear()
    {
        return Faker.Date.Future().Year.ToString();
    }

    public static string RandomEmail()
    {
        const string domain = "example.com";
        var userName = Faker.Random.String2(8, "ABCDEFGHIJKLMNOPQRSTUVWXYZ").ToLower();

        var email = $"{userName}@{domain}";
        return email;
    }

    public static string RandomPartsNumber(int digits = 1, bool testMode = true)
    {
        if (testMode)
        {
            return "1";
        }

        return digits switch
        {
            1 => Faker.Random.Number(2, 9).ToString(),
            2 => Faker.Random.Number(11, 20).ToString(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(digits),
                "Only 1 or 2 digits are supported, up to 20 maximum."
            )
        };
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

    public static TaskTypeKind RandomTaskTypeKind(TaskTypeKind? exclude = null) => exclude is null
        ? RandomEnum<TaskTypeKind>()
        : RandomEnum(exclude.Value);

    public static List<TaskTypeKind> RandomTaskTypeKinds(int count, IEnumerable<TaskTypeKind>? exclude = null)
        => RandomEnumMany(count, exclude);
}