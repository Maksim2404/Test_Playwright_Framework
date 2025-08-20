namespace test.playwright.framework.pages.reports;

public readonly record struct DateRange(DateTimeOffset Start, DateTimeOffset End)
{
    public override string ToString() => $"{Start:yyyy-MM-dd}..{End:yyyy-MM-dd}";
}

public static class ReportDateRanges
{
    public static DateRange CurrentMonthToToday(DateTimeOffset now)
    {
        var start = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
        // UI shows today as the end (not end-of-month)
        var end = new DateTimeOffset(now.Year, now.Month, now.Day, 23, 59, 59, now.Offset);
        return new DateRange(start, end);
    }

    // monthsBack = 1 => last month; 2 => two months ago, etc.
    public static DateRange MonthNBack(DateTimeOffset now, int monthsBack)
    {
        var firstOfThisMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
        var start = firstOfThisMonth.AddMonths(-monthsBack);
        var end = start.AddMonths(1).AddTicks(-1); // inclusive end of that month
        return new DateRange(start, end);
    }

    public static string MonthChipLabelForNBack(DateTimeOffset now, int monthsBack) =>
        MonthNBack(now, monthsBack).Start.ToString("MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
}
