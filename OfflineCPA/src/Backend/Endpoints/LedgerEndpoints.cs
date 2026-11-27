using Application.Interfaces;
using Application.Ledger;
using Domain.Ledger;

namespace Backend.Endpoints;

public static class LedgerEndpoints
{
    public static void MapLedgerEndpoints(this IEndpointRouteBuilder app)
    {
        // Ledger endpoints: thin. Bind, call one use case, map rule violations to 400.
        // No auth until a later prompt, so every ledger action is attributed to the constant "local" user.
        const string LocalUserId = "local";

        app.MapPost("/api/ledger/entries", async (CreateEntryRequest request, CreateDraftEntry useCase, IPiiMasker masker, CancellationToken ct) =>
        {
            try
            {
                var lines = request.Lines.Select(l => new DraftPostingLine(l.AccountId, l.Amount, l.Memo)).ToList();
                var id = await useCase.HandleAsync(new CreateDraftEntryCommand(request.Date, request.Memo, request.PeriodId, lines, LocalUserId), ct);
                return Results.Ok(new { id });
            }
            catch (LedgerRuleException ex) { return Results.BadRequest(new { error = masker.Mask(ex.Message) }); }
        })
        .WithName("CreateLedgerEntry")
        .WithOpenApi();

        app.MapPost("/api/ledger/entries/{id:guid}/post", async (Guid id, PostEntry useCase, IPiiMasker masker, CancellationToken ct) =>
        {
            try
            {
                await useCase.HandleAsync(new PostEntryCommand(id, LocalUserId), ct);
                return Results.Ok(new { id });
            }
            catch (LedgerRuleException ex) { return Results.BadRequest(new { error = masker.Mask(ex.Message) }); }
        })
        .WithName("PostLedgerEntry")
        .WithOpenApi();

        app.MapPost("/api/ledger/entries/{id:guid}/reverse", async (Guid id, ReverseEntryRequest request, ReverseEntry useCase, IPiiMasker masker, CancellationToken ct) =>
        {
            try
            {
                var reversalId = await useCase.HandleAsync(new ReverseEntryCommand(id, request.ReversalDate, request.Memo, LocalUserId), ct);
                return Results.Ok(new { id = reversalId });
            }
            catch (LedgerRuleException ex) { return Results.BadRequest(new { error = masker.Mask(ex.Message) }); }
        })
        .WithName("ReverseLedgerEntry")
        .WithOpenApi();

        app.MapPost("/api/ledger/periods/{id:guid}/lock", async (Guid id, LockPeriod useCase, IPiiMasker masker, CancellationToken ct) =>
        {
            try
            {
                await useCase.HandleAsync(new LockPeriodCommand(id, LocalUserId), ct);
                return Results.Ok(new { id });
            }
            catch (LedgerRuleException ex) { return Results.BadRequest(new { error = masker.Mask(ex.Message) }); }
        })
        .WithName("LockLedgerPeriod")
        .WithOpenApi();

        app.MapGet("/api/ledger/balances", async (GetAccountBalances useCase, IPiiMasker masker, CancellationToken ct) =>
        {
            try { return Results.Ok(await useCase.HandleAsync(ct)); }
            catch (LedgerRuleException ex) { return Results.BadRequest(new { error = masker.Mask(ex.Message) }); }
        })
        .WithName("GetLedgerBalances")
        .WithOpenApi();
    }
}
