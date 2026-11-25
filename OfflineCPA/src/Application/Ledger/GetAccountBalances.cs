using Application.Interfaces;

namespace Application.Ledger;

public sealed class GetAccountBalances
{
    private readonly ILedgerStore _store;

    public GetAccountBalances(ILedgerStore store) => _store = store;

    public Task<IReadOnlyList<AccountBalance>> HandleAsync(CancellationToken ct = default)
        => _store.GetAccountBalancesAsync(ct);
}
