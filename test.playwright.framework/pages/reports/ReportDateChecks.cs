using System.Globalization;
using FluentAssertions;
using test.playwright.framework.pages.enums;

namespace test.playwright.framework.pages.reports;

public static class ReportDateChecks
{
    private static bool TryParseCompletedDateSmart(string? s, DateRange expectedRange, out DateTime dt)
    {
        s = (s ?? "").Trim();
        dt = default;

        // Day-first formats (dd-MM-…)
        var dayFirst = new[]
        {
            "d-M-yy H:mm", "d-M-yy HH:mm", "d-M-yyyy H:mm", "d-M-yyyy HH:mm", "d-M-yyyy h:mm tt", "d-M-yy h:mm tt",
            "d-M-yy", "d-M-yyyy"
        };

        // Month-first formats (MM-dd-…)
        var monthFirst = new[]
        {
            "M-d-yy H:mm", "M-d-yy HH:mm", "M-d-yyyy H:mm", "M-d-yyyy HH:mm", "M-d-yyyy h:mm tt", "M-d-yy h:mm tt",
            "M-d-yy", "M-d-yyyy"
        };

        var ci = CultureInfo.InvariantCulture;
        const DateTimeStyles styles = DateTimeStyles.AllowWhiteSpaces;

        var okDay = TryAny(dayFirst, out var d1);
        var okMon = TryAny(monthFirst, out var d2);

        switch (okDay)
        {
            case false when !okMon:
                return false;
            // If only one parsed, use it
            case true when !okMon:
                dt = d1;
                return true;
            case false when okMon:
                dt = d2;
                return true;
        }

        // Both parsed — choose the one that falls inside the expected range (date-only)
        var start = expectedRange.Start.Date;
        var end = expectedRange.End.Date;

        var d1In = d1.Date >= start && d1.Date <= end;
        var d2In = d2.Date >= start && d2.Date <= end;

        switch (d1In)
        {
            case true when !d2In:
                dt = d1;
                return true;
            case false when d2In:
            case true when d2In:
                dt = d2;
                return true;
        }

        // Neither candidate in range — return false so the can fail test with context
        return false;

        bool TryAny(string[] fmts, out DateTime parsed) =>
            DateTime.TryParseExact(s, fmts, ci, styles, out parsed);
    }

    public static void DatesShouldFallWithinMonth(string[] headers, List<string[]> rows, DateRange monthRange,
        bool reportTimesAreUtc = true)
    {
        var idx = ReportTable.ColIndex(headers, ReportColumnKInd.CompletedDate.ToUi());
        idx.Should().BeGreaterThanOrEqualTo(0, "Sheet must include 'Completed Date'.");

        foreach (var cell in rows.Select(r => r[idx]))
        {
            if (!TryParseCompletedDateSmart(cell, monthRange, out var parsed))
                throw new InvalidOperationException(
                    $"Cannot parse Completed Date '{cell}' into a value within {monthRange}.");

            //It looks like report timestamps are UTC, have to handle that:
            DateTimeOffset local;
            if (reportTimesAreUtc)
            {
                var asUtc = new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Utc));
                local = asUtc.ToOffset(monthRange.Start.Offset);
            }
            else
            {
                //Treat as already-local:
                local = new DateTimeOffset(parsed, monthRange.Start.Offset);
            }

            var dateOnly = local.Date;

            // Optional grace for midnight-crossing: if it's exactly next day but < 06:00 local, count it in-range
            if (dateOnly == monthRange.End.Date.AddDays(1) && local.TimeOfDay < TimeSpan.FromHours(6))
                dateOnly = monthRange.End.Date;

            dateOnly.Should().BeOnOrAfter(monthRange.Start.Date)
                .And.BeOnOrBefore(monthRange.End.Date,
                    $"Completed Date '{cell}' (local {local:yyyy-MM-dd HH:mm}) should fall within {monthRange}.");
        }
    }
}
