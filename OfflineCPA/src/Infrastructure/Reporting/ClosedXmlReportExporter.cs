using Application.Interfaces;
using ClosedXML.Excel;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Reporting;

/// <summary>
/// Renders today's transaction log to a local workbook. This is a hold-over
/// view for the file upload flow; the CPA report packet (a later prompt)
/// renders from posted ledger entries instead.
/// </summary>
public sealed class ClosedXmlReportExporter : IReportExporter
{
    private readonly OfflineCPADbContext _db;

    public ClosedXmlReportExporter(OfflineCPADbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> ExportTransactionLogAsync(CancellationToken cancellationToken = default)
    {
        var transactions = await _db.Transactions.OrderBy(t => t.Date).ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("1120-S Bookkeeping");

        worksheet.Cell(1, 1).Value = "Date";
        worksheet.Cell(1, 2).Value = "Description";
        worksheet.Cell(1, 3).Value = "Document Path";

        var row = 2;
        foreach (var txn in transactions)
        {
            worksheet.Cell(row, 1).Value = txn.Date.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 2).Value = txn.Description;
            worksheet.Cell(row, 3).Value = txn.ReferenceDocumentPath ?? "N/A";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
