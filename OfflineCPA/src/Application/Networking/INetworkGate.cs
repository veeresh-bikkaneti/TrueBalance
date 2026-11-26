namespace Application.Networking;

/// <summary>
/// Port for the network gate. Every outbound HTTP call must go through this
/// gate. The default behavior denies all calls; an adapter gets an
/// <see cref="HttpClient"/> only when the user has enabled that integration in
/// settings, and only for the one host it asked for. Each attempt, allowed or
/// denied, is recorded in the local event log.
/// </summary>
public interface INetworkGate
{
    /// <summary>
    /// Requests an HTTP client for one host on behalf of one integration.
    /// Returns null when the integration is disabled or the request is
    /// invalid; the caller must not fall back to its own HTTP client.
    /// </summary>
    Task<HttpClient?> TryOpenClientAsync(NetworkGateRequest request, CancellationToken cancellationToken = default);
}

/// <summary>One adapter asking for one host. Both values are sanitized before logging.</summary>
public sealed record NetworkGateRequest(string AdapterName, string Host);

/// <summary>
/// User opt-in for integrations. No integration is enabled until the user
/// switches it on in settings.
/// </summary>
public interface IIntegrationSettings
{
    Task<bool> IsEnabledAsync(string adapterName, CancellationToken cancellationToken = default);
}

/// <summary>Reads as: at <see cref="OccurredAtUtc"/>, adapter X asked for host Y, and it was allowed or denied.</summary>
public sealed record NetworkGateAttempt(string AdapterName, string Host, bool Allowed, DateTime OccurredAtUtc);

/// <summary>Local, append-only record of every gate attempt. This is the only place the gate logs.</summary>
public interface INetworkAttemptLog
{
    Task AppendAsync(NetworkGateAttempt attempt, string localUserId, CancellationToken cancellationToken = default);
}
