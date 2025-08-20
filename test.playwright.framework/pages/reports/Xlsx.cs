using ClosedXML.Excel;

namespace test.playwright.framework.pages.reports;

public static class Xlsx
{
    private static readonly StringComparer Ci = StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Reads the first worksheet and auto-detects the header row by looking for expected header names.
    /// </summary>
    public static (string[] Headers, List<string[]> Rows) ReadFirstSheetAutoHeader(Stream stream,
        string[] expectedHeaderNames, int scanTopRows = 20)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1);
        var used = ws.RangeUsed();
        if (used is null) return ([], []);

        // Collect used rows
        var usedRows = used.RowsUsed().ToList();
        if (usedRows.Count == 0) return ([], []);

        // Find header row: the first row that contains at least 3 expected headers (tweak threshold if needed)
        var headerRowIndexInUsed = -1;
        string[] headerTexts = [];

        for (var i = 0; i < Math.Min(scanTopRows, usedRows.Count); i++)
        {
            var r = usedRows[i];
            var cells = r.CellsUsed().ToList();
            if (cells.Count == 0) continue;

            var texts = cells.Select(c => Normalize(c.GetString())).ToArray();

            // Count how many expected headers appear in this row
            var hits = texts.Count(t => expectedHeaderNames.Any(e => Ci.Equals(e, t)));
            if (hits < 3) continue; // threshold
            headerRowIndexInUsed = i;
            headerTexts = texts;
            break;
        }

        if (headerRowIndexInUsed < 0)
        {
            // Fallback: take the first non-empty row as headers
            var first = usedRows[0];
            headerTexts = first.CellsUsed().Select(c => Normalize(c.GetString())).ToArray();
            headerRowIndexInUsed = 0;
        }

        // Build data rows from the next row onwards
        var data = new List<string[]>();
        for (var i = headerRowIndexInUsed + 1; i < usedRows.Count; i++)
        {
            var r = usedRows[i];
            // read up to header length; format cells (to respect Excel number/date formats)
            var rowVals = r.Cells(1, headerTexts.Length)
                .Select(c => Normalize(c.GetFormattedString()))
                .ToArray();

            // skip completely empty rows
            if (rowVals.All(string.IsNullOrWhiteSpace)) continue;

            data.Add(rowVals);
        }

        return (headerTexts, data);

        static string Normalize(string? s)
        {
            // trim and collapse inner whitespace; keep plain case for display, we match with CI anyway
            var t = (s ?? string.Empty).Trim();
            return System.Text.RegularExpressions.Regex.Replace(t, @"\s+", " ");
        }
    }
}
