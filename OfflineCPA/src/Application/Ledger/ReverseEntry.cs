using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Ledger;

namespace Application.Ledger;

public sealed record ReverseEntryCommand(Guid EntryId, DateOnly ReversalDate, string Memo, string LocalUserId);

public sealed class ReverseEntry
{
    private readonly ILedgerStore _store;
    private readonly IClock _clock;

    public ReverseEntry(ILedgerStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    /// <returns>The id of the new reversing entry.</returns>
    public async Task<Guid> HandleAsync(ReverseEntryCommand command, CancellationToken ct = default)
    {
        var entry = await _store.FindEntryAsync(command.EntryId, ct)
            ?? throw new LedgerRuleException($"Entry {command.EntryId} does not exist.");
        var period = await _store.FindPeriodForDateAsync(command.ReversalDate, ct)
            ?? throw new LedgerRuleException($"No period covers reversal date {command.ReversalDate:yyyy-MM-dd}.");
        var now = _clock.UtcNow;

        var reversal = entry.Reverse(period, command.ReversalDate, command.Memo, now);

        await _store.AddEntryAsync(reversal, ct);
        await _store.AppendAuditAsync(LedgerAuditRecord.For(LedgerAction.Reversed, command.LocalUserId, now,
            journalEntryId: reversal.Id, periodId: period.Id, details: $"Reverses entry {entry.Id}."), ct);
        await _store.SaveChangesAsync(ct);
        return reversal.Id;
    }
}
