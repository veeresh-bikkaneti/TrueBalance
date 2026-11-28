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
    private readonly IPiiMasker _masker;

    public CreateDraftEntry(ILedgerStore store, IClock clock, IPiiMasker masker)
    {
        _store = store;
        _clock = clock;
        _masker = masker;
    }

    public async Task<Guid> HandleAsync(CreateDraftEntryCommand command, CancellationToken ct = default)
    {
        var period = await _store.FindPeriodAsync(command.PeriodId, ct)
            ?? throw new LedgerRuleException($"Period {command.PeriodId} does not exist.");
        var now = _clock.UtcNow;
        var postings = (command.Lines ?? Array.Empty<DraftPostingLine>())
            .Select(l => Posting.Create(l.AccountId, l.Amount, l.Memo is null ? null : _masker.Mask(l.Memo)))
            .ToList();
        // A missing memo becomes empty so the entry raises its own "memo is required" rule.
        var memo = _masker.Mask(command.Memo ?? string.Empty);
        var entry = JournalEntry.CreateDraft(command.Date, memo, period, postings, now);

        await _store.AddEntryAsync(entry, ct);
        await new DomainEventDispatcher(_store).DispatchAsync(entry.PullEvents(), command.LocalUserId, now, ct);
        await _store.SaveChangesAsync(ct);
        return entry.Id;
    }
}
