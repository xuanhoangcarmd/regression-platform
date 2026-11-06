namespace RegressionPlatform.Application.Features.RequestPool.Contracts;

/// <summary>
/// Tabular data parsed from an uploaded Excel file: the ordered column headers and one
/// column → cell-value map per data row. Cell values are typed per the Excel cell type
/// (number → numeric, boolean → bool, otherwise string) so they serialize with the right JSON type.
/// </summary>
public sealed record ExcelImportData(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows
);
