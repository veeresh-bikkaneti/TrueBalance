using Application.Interfaces;
using Domain.Ledger;

namespace Application.Ledger;

public sealed record PostStagedRowCommand(
    Guid RowId, Guid CategoryAccountId, bool AllowDuplicate, bool Reviewed, string LocalUserId);

/// <summary>Turns one staged row into one balanced entry: bank account against the chosen category.</summary>
public sealed class PostStagedRow(
    IImportStore imports, ILedgerStore ledger, CreateDraftEntry createDraft, PostEntry postEntry, IClock clock)
{
    public async Task<Guid> HandleAsync(PostStagedRowCommand command, CancellationToken ct = default)
    {
        var row = await imports.FindRowAsync(command.RowId, ct)
            ?? throw new LedgerRuleException("The staged row does not exist.");
        row.EnsureCanPost(command.AllowDuplicate, command.Reviewed);
        if (command.CategoryAccountId == row.BankAccountId
            || !await imports.AccountExistsAsync(command.CategoryAccountId, ct))
            throw new LedgerRuleException("Pick an existing category account other than the bank account.");
        if (row.Amount == 0m)
            throw new LedgerRuleException("A zero amount row cannot be posted.");
        var period = await ledger.FindPeriodForDateAsync(row.Date, ct)
            ?? throw new LedgerRuleException($"No period covers {row.Date:yyyy-MM-dd}.");

        // Positive amounts are debits: money in debits the bank account.
        var memo = row.Description.Length == 0 ? "Imported statement row" : row.Description;
        var entryId = await createDraft.HandleAsync(new CreateDraftEntryCommand(row.Date, memo, period.Id,
            new[]
            {
                new DraftPostingLine(row.BankAccountId, row.Amount, null),
                new DraftPostingLine(command.CategoryAccountId, -row.Amount, null)
            }, command.LocalUserId), ct);

        // Marked before PostEntry saves, so the row and the posted entry commit together.
        row.MarkPosted(entryId, command.CategoryAccountId, clock.UtcNow);
        await postEntry.HandleAsync(new PostEntryCommand(entryId, command.LocalUserId), ct);
        return entryId;
    }
}
