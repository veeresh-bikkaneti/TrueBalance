namespace Application.Interfaces;

/// <summary>
/// Port for a read-only report export. An adapter renders a workbook from data
/// the app already holds; it never reads raw uploaded files and never opens a
/// network connection.
/// </summary>
public interface IReportExporter
{
    Task<byte[]> ExportTransactionLogAsync(CancellationToken cancellationToken = default);
}
