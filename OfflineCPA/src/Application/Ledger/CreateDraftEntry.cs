using Application.Interfaces;
using Domain.Entities;
using Domain.Ledger;

namespace Application.Ledger;

public sealed record DraftPostingLine(Guid AccountId, decimal Amount, string? Memo);

public sealed record CreateDraftEntryCommand(
    DateOnly Date, string Memo, Guid PeriodId, IReadOnlyList<DraftPostingLine> Lines, string LocalUserId);

public sealed class CreateDraftEntry
{
    private readonly ILedgerStore _store;
    private readonly IClock _clock;

    public CreateDraftEntry(ILedgerStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task<Guid> HandleAsync(CreateDraftEntryCommand command, CancellationToken ct = default)
    {
        var period = await _store.FindPeriodAsync(command.PeriodId, ct)
            ?? throw new LedgerRuleException($"Period {command.PeriodId} does not exist.");
        var now = _clock.UtcNow;
        var postings = (command.Lines ?? Array.Empty<DraftPostingLine>())
            .Select(l => Posting.Create(l.AccountId, l.Amount, l.Memo));
        var entry = JournalEntry.CreateDraft(command.Date, command.Memo, period, postings, now);

        await _store.AddEntryAsync(entry, ct);
        await new DomainEventDispatcher(_store).DispatchAsync(entry.PullEvents(), command.LocalUserId, now, ct);
        await _store.SaveChangesAsync(ct);
        return entry.Id;
    }
}
