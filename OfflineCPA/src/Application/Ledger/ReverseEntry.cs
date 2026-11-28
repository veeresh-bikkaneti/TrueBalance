using Application.Interfaces;
using Domain.Ledger;

namespace Application.Ledger;

public sealed record ReverseEntryCommand(Guid EntryId, DateOnly ReversalDate, string Memo, string LocalUserId);

public sealed class ReverseEntry
{
    private readonly ILedgerStore _store;
    private readonly IClock _clock;
    private readonly IPiiMasker _masker;

    public ReverseEntry(ILedgerStore store, IClock clock, IPiiMasker masker)
    {
        _store = store;
        _clock = clock;
        _masker = masker;
    }

    /// <returns>The id of the new reversing entry.</returns>
    public async Task<Guid> HandleAsync(ReverseEntryCommand command, CancellationToken ct = default)
    {
        var entry = await _store.FindEntryAsync(command.EntryId, ct)
            ?? throw new LedgerRuleException($"Entry {command.EntryId} does not exist.");
        var period = await _store.FindPeriodForDateAsync(command.ReversalDate, ct)
            ?? throw new LedgerRuleException($"No period covers reversal date {command.ReversalDate:yyyy-MM-dd}.");
        var now = _clock.UtcNow;

        // Line memos are copied from the original entry, which masked them when it was drafted.
        var memo = _masker.Mask(command.Memo ?? string.Empty);

        var reversal = entry.Reverse(period, command.ReversalDate, memo, now);

        await _store.AddEntryAsync(reversal, ct);
        await new DomainEventDispatcher(_store).DispatchAsync(entry.PullEvents(), command.LocalUserId, now, ct);
        await _store.SaveChangesAsync(ct);
        return reversal.Id;
    }
}
