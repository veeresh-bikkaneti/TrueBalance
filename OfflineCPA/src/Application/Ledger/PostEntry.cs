using Application.Interfaces;
using Domain.Ledger;

namespace Application.Ledger;

public sealed record PostEntryCommand(Guid EntryId, string LocalUserId);

public sealed class PostEntry
{
    private readonly ILedgerStore _store;
    private readonly IClock _clock;

    public PostEntry(ILedgerStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task HandleAsync(PostEntryCommand command, CancellationToken ct = default)
    {
        var entry = await _store.FindEntryAsync(command.EntryId, ct)
            ?? throw new LedgerRuleException($"Entry {command.EntryId} does not exist.");
        var period = await _store.FindPeriodAsync(entry.PeriodId, ct)
            ?? throw new LedgerRuleException($"Period {entry.PeriodId} does not exist.");
        var now = _clock.UtcNow;

        entry.Post(period, now);

        await new DomainEventDispatcher(_store).DispatchAsync(entry.PullEvents(), command.LocalUserId, now, ct);
        await _store.SaveChangesAsync(ct);
    }
}
