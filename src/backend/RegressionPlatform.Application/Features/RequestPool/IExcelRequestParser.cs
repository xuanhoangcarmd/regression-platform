using RegressionPlatform.Application.Features.RequestPool.Contracts;

namespace RegressionPlatform.Application.Features.RequestPool;

/// <summary>Parses an uploaded Excel (.xlsx) stream into tabular rows. Implemented in Infrastructure.</summary>
public interface IExcelRequestParser
{
    /// <summary>Reads the first worksheet: header row = column names, each non-empty row = one record.</summary>
    ExcelImportData Parse(Stream file);
}
