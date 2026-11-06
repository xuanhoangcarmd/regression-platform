using ClosedXML.Excel;
using RegressionPlatform.Application.Features.RequestPool;
using RegressionPlatform.Application.Features.RequestPool.Contracts;

namespace RegressionPlatform.Infrastructure.Services;

/// <summary>
/// Reads an uploaded .xlsx file with ClosedXML: the data worksheet's first used row supplies the
/// column names, and each subsequent non-empty row becomes a record keyed by those columns.
/// The data worksheet is the first one whose header row has a "Title" column (so an empty or notes
/// sheet in front doesn't break the import); otherwise the first worksheet with any content.
/// </summary>
public sealed class ExcelRequestParser : IExcelRequestParser
{
    private const string TitleHeader = "Title";

    public ExcelImportData Parse(Stream file)
    {
        using var workbook = new XLWorkbook(file);
        var sheets = workbook.Worksheets
            .Select(sheet => sheet.RangeUsed()?.RowsUsed().ToList())
            .Where(rows => rows is { Count: > 0 })
            .Select(rows => rows!)
            .ToList();
        var rows = sheets.FirstOrDefault(HasTitleHeader) ?? sheets.FirstOrDefault();
        if (rows is null)
        {
            return new ExcelImportData([], []);
        }

        // Header row -> (column name, worksheet column number); skip blank headers.
        var headerCells = rows[0]
            .CellsUsed()
            .Select(cell => (Name: cell.GetString().Trim(), Column: cell.Address.ColumnNumber))
            .Where(header => header.Name.Length > 0)
            .ToList();

        var columns = headerCells.Select(header => header.Name).Distinct(StringComparer.Ordinal).ToList();

        var dataRows = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var row in rows.Skip(1))
        {
            var values = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var header in headerCells)
            {
                values[header.Name] = CellValue(row.Cell(header.Column));
            }

            // Skip rows where every cell is empty.
            if (values.Values.Any(HasValue))
            {
                dataRows.Add(values);
            }
        }

        return new ExcelImportData(columns, dataRows);
    }

    private static bool HasTitleHeader(List<IXLRangeRow> rows) =>
        rows[0].CellsUsed().Any(cell => string.Equals(cell.GetString().Trim(), TitleHeader, StringComparison.OrdinalIgnoreCase));

    /// <summary>Reads a cell as a typed value matching its Excel data type (the column's format).</summary>
    private static object? CellValue(IXLCell cell) =>
        cell.DataType switch
        {
            XLDataType.Number => NormalizeNumber(cell.GetDouble()),
            XLDataType.Boolean => cell.GetBoolean(),
            XLDataType.DateTime => cell.GetDateTime().ToString("o"),
            XLDataType.TimeSpan => cell.GetTimeSpan().ToString(),
            // Text / Blank / Error → the displayed string (so Text-formatted "007" stays a string).
            _ => cell.GetString().Trim(),
        };

    // Integral numbers become long (exact, avoids 1.0 / float noise); the rest stay double.
    private static object NormalizeNumber(double value) =>
        value == Math.Floor(value) && value >= long.MinValue && value <= long.MaxValue ? (long)value : value;

    private static bool HasValue(object? value) =>
        value switch
        {
            null => false,
            string s => s.Length > 0,
            _ => true,
        };
}
