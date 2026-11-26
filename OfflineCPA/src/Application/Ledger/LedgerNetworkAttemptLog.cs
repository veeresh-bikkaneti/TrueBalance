using Application.Interfaces;
using Application.Networking;
using Domain.Enums;
using Domain.Ledger;

namespace Application.Ledger;

/// <summary>
/// Writes one gate attempt to the append-only event log. The summary carries
/// only the sanitized adapter name and host, never a path, a query, a
/// personal name, or any other raw value.
/// </summary>
public sealed class LedgerNetworkAttemptLog : INetworkAttemptLog
{
    public const int MaxAdapterNameLength = 40;
    public const int MaxHostLength = 100;

    private readonly ILedgerStore _store;

    public LedgerNetworkAttemptLog(ILedgerStore store)
    {
        _store = store;
    }

    public async Task AppendAsync(NetworkGateAttempt attempt, string localUserId, CancellationToken cancellationToken = default)
    {
        var record = Domain.Entities.LedgerAuditRecord.For(
            attempt.Allowed ? LedgerAction.NetworkGateGranted : LedgerAction.NetworkGateDenied,
            attempt.Allowed ? LedgerEventType.NetworkGranted : LedgerEventType.NetworkDenied,
            localUserId,
            attempt.OccurredAtUtc,
            details: $"network {(attempt.Allowed ? "granted" : "denied")}: {Sanitize(attempt.AdapterName, MaxAdapterNameLength)} {Sanitize(attempt.Host, MaxHostLength)}");

        await _store.AppendAuditAsync(record, cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Keeps only a safe shape: lowercase letters, digits, dots, dashes, and colons. Handles null and trims to the cap.</summary>
    private static string Sanitize(string? value, int maxLength)
    {
        var cleaned = new string((value ?? string.Empty).ToLowerInvariant()
            .Where(Allowed)
            .ToArray());
        return cleaned.Length <= maxLength ? cleaned : cleaned[..maxLength];
    }

    private static bool Allowed(char c) => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '-' or ':';
}
