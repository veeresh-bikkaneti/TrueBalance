using Application.Interfaces;
using Domain.Ledger;

namespace Application.Ledger;

public sealed record LockPeriodCommand(Guid PeriodId, string LocalUserId);

public sealed class LockPeriod
{
    private readonly ILedgerStore _store;
    private readonly IClock _clock;

    public LockPeriod(ILedgerStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task HandleAsync(LockPeriodCommand command, CancellationToken ct = default)
    {
        var period = await _store.FindPeriodAsync(command.PeriodId, ct)
            ?? throw new LedgerRuleException($"Period {command.PeriodId} does not exist.");
        var now = _clock.UtcNow;

        period.Lock();

        await new DomainEventDispatcher(_store).DispatchAsync(period.PullEvents(), command.LocalUserId, now, ct);
        await _store.SaveChangesAsync(ct);
    }
}
