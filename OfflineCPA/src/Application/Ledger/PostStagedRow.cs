using Application.Interfaces;
using Domain.Entities;
using Domain.Ledger;

namespace Application.Ledger;

public sealed record PostStagedRowCommand(
    Guid RowId, Guid CategoryAccountId, bool AllowDuplicate, bool Reviewed, string LocalUserId);

/// <summary>Turns one staged row into one balanced entry: bank account against the chosen category.</summary>
public sealed class PostStagedRow(IImportStore imports, ILedgerStore ledger, IClock clock)
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
        // Both ledger use cases run against one buffered store, so the draft, the
        // post and the row are saved together or not at all.
        var once = new SingleSaveLedger(ledger);
        var entryId = await new CreateDraftEntry(once, clock).HandleAsync(new CreateDraftEntryCommand(row.Date, memo, period.Id,
            new[]
            {
                new DraftPostingLine(row.BankAccountId, row.Amount, null),
                new DraftPostingLine(command.CategoryAccountId, -row.Amount, null)
            }, command.LocalUserId), ct);

        await new PostEntry(once, clock).HandleAsync(new PostEntryCommand(entryId, command.LocalUserId), ct);
        row.MarkPosted(entryId, command.CategoryAccountId, clock.UtcNow);
        await ledger.SaveChangesAsync(ct);
        return entryId;
    }
}

/// <summary>Holds back saves and finds the entry that is not saved yet.</summary>
internal sealed class SingleSaveLedger(ILedgerStore inner) : ILedgerStore
{
    private JournalEntry? _added;

    public Task AddEntryAsync(JournalEntry entry, CancellationToken ct = default)
    {
        _added = entry;
        return inner.AddEntryAsync(entry, ct);
    }

    public Task<JournalEntry?> FindEntryAsync(Guid id, CancellationToken ct = default)
        => _added?.Id == id ? Task.FromResult<JournalEntry?>(_added) : inner.FindEntryAsync(id, ct);

    public Task<Period?> FindPeriodAsync(Guid id, CancellationToken ct = default)
        => inner.FindPeriodAsync(id, ct);

    public Task<Period?> FindPeriodForDateAsync(DateOnly date, CancellationToken ct = default)
        => inner.FindPeriodForDateAsync(date, ct);

    public Task<IReadOnlyList<AccountBalance>> GetAccountBalancesAsync(CancellationToken ct = default)
        => inner.GetAccountBalancesAsync(ct);

    public Task AppendAuditAsync(LedgerAuditRecord record, CancellationToken ct = default)
        => inner.AppendAuditAsync(record, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
