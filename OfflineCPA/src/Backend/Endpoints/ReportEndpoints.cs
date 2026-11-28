using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Endpoints;

public sealed class ReportEndpoints : IEndpointModule
{
    public void AddServices(IServiceCollection services) { }

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transactions", async (OfflineCPADbContext db) =>
        {
            return await db.Transactions.OrderByDescending(t => t.Date).ToListAsync();
        })
        .WithName("GetTransactions")
        .WithOpenApi();

        app.MapGet("/api/export", async (IReportExporter exporter, CancellationToken cancellationToken) =>
        {
            var bytes = await exporter.ExportTransactionLogAsync(cancellationToken);
            return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"OfflineCPA_Export_{DateTime.Now:yyyyMMdd}.xlsx");
        })
        .WithName("ExportToExcel")
        .WithOpenApi();
    }
}
