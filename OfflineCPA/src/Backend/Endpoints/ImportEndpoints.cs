using Application.Imports;
using Application.Interfaces;
using Application.Ledger;
using Domain.Ledger;
using Microsoft.EntityFrameworkCore;

namespace Backend.Endpoints;

public sealed record ImportBatchRequest(Guid BankAccountId, string? BankName, ConfirmedLayout? Layout = null);

public sealed record PostRowRequest(Guid CategoryAccountId, bool AllowDuplicate = false, bool Reviewed = false);

public sealed record SaveProfileRequest(string BankName, CsvColumnMap Columns);

/// <summary>Thin import routes: bind, call one use case, map rule violations to 400 or 409.</summary>
public static class ImportEndpoints
{
    public static void MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        // Watched files get their batch id here; there is no other way to learn it.
        app.MapGet("/api/imports", async (IImportStore store, CancellationToken ct)
            => Results.Ok(await store.GetBatchesAsync(ct)));

        app.MapPost("/api/imports/{batchId:guid}/import",
            (Guid batchId, ImportBatchRequest request, ImportStatement useCase, IPiiMasker masker, CancellationToken ct)
                => RunAsync(masker, async () => new
                {
                    rows = await useCase.HandleAsync(new ImportStatementCommand(
                        batchId, request.BankAccountId, request.BankName, request.Layout), ct)
                }));

        app.MapGet("/api/imports/{batchId:guid}/layout-preview",
            (Guid batchId, string? bankName, PreviewCsvLayout useCase, IPiiMasker masker, CancellationToken ct)
                => RunAsync(masker, () => useCase.HandleAsync(batchId, bankName, ct)));

        app.MapGet("/api/imports/{batchId:guid}/rows", async (Guid batchId, IImportStore store, CancellationToken ct) =>
        {
            var result = new List<object>();
            foreach (var row in await store.GetRowsAsync(batchId, ct))
            {
                result.Add(new { row, suggestedCategoryAccountId = await store.LastPostedCategoryAsync(row.MatchKey, ct) });
            }
            return Results.Ok(result);
        });

        app.MapPost("/api/imports/rows/{rowId:guid}/post",
            (Guid rowId, PostRowRequest request, PostStagedRow useCase, IPiiMasker masker, CancellationToken ct)
                => RunAsync(masker, async () => new
                {
                    id = await useCase.HandleAsync(new PostStagedRowCommand(
                        rowId, request.CategoryAccountId, request.AllowDuplicate, request.Reviewed, "local"), ct)
                }));

        app.MapPut("/api/imports/profiles",
            (SaveProfileRequest request, SaveCsvProfile useCase, IPiiMasker masker, CancellationToken ct)
                => RunAsync(masker, async () =>
                {
                    await useCase.HandleAsync(request.BankName, request.Columns, ct);
                    return new { saved = true };
                }));
    }

    private static async Task<IResult> RunAsync<T>(IPiiMasker masker, Func<Task<T>> action)
    {
        try
        {
            return Results.Ok(await action());
        }
        catch (LedgerRuleException ex)
        {
            return Results.BadRequest(new { error = masker.Mask(ex.Message) });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { error = "Someone else changed this item first. Reload and try again." });
        }
    }
}
