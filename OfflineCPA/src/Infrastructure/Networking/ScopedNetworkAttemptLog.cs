using Application.Interfaces;
using Application.Ledger;
using Application.Networking;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Networking;

/// <summary>
/// Decorator that gives each gate log write its own scope, so the append and
/// its save touch a fresh short-lived ledger store. A gate call made in the
/// middle of a use case can no longer commit that use case's pending changes.
/// </summary>
public sealed class ScopedNetworkAttemptLog : INetworkAttemptLog
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ScopedNetworkAttemptLog(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task AppendAsync(NetworkGateAttempt attempt, string localUserId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var logger = new LedgerNetworkAttemptLog(scope.ServiceProvider.GetRequiredService<ILedgerStore>());
        await logger.AppendAsync(attempt, localUserId, cancellationToken);
    }
}
