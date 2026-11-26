using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Application.Interfaces;
using Application.Networking;

namespace Infrastructure.Networking;

/// <summary>
/// The default network gate. It denies every request unless the user has
/// enabled that integration in settings, logs every attempt, and hands out an
/// HttpClient that is locked to the one approved host. Nothing else in the
/// app is allowed to create an HttpClient.
/// </summary>
public sealed partial class LocalNetworkGate : INetworkGate
{
    [GeneratedRegex("^[a-z0-9]([a-z0-9-]*[a-z0-9])?(\\.[a-z0-9]([a-z0-9-]*[a-z0-9])?)*(:[0-9]{1,5})?$")]
    private static partial Regex HostPattern();

    private readonly IIntegrationSettings _settings;
    private readonly INetworkAttemptLog _attemptLog;
    private readonly IClock _clock;
    private readonly string _localUserId;

    public LocalNetworkGate(IIntegrationSettings settings, INetworkAttemptLog attemptLog, IClock clock, string localUserId = "local")
    {
        _settings = settings;
        _attemptLog = attemptLog;
        _clock = clock;
        _localUserId = localUserId;
    }

    public async Task<HttpClient?> TryOpenClientAsync(NetworkGateRequest request, CancellationToken cancellationToken = default)
    {
        var adapterName = (request.AdapterName ?? string.Empty).Trim().ToLowerInvariant();
        var host = (request.Host ?? string.Empty).Trim().ToLowerInvariant();
        var allowed = adapterName.Length != 0
            && host.Length != 0
            && HostPattern().IsMatch(host)
            && await _settings.IsEnabledAsync(adapterName, cancellationToken);

        await _attemptLog.AppendAsync(new NetworkGateAttempt(
            adapterName.Length == 0 ? "unknown" : adapterName,
            host.Length == 0 ? "invalid" : host,
            allowed,
            _clock.UtcNow), _localUserId, cancellationToken);

        if (!allowed)
            return null;

        var client = new HttpClient(CreateHostLockingHandler(ApprovedHostName(host)))
        {
            BaseAddress = new Uri($"https://{host}/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        return client;
    }

    /// <summary>Strips an explicit port so the connect check compares against the host name the framework reports.</summary>
    private static string ApprovedHostName(string host)
    {
        var colon = host.IndexOf(':');
        return colon >= 0 ? host[..colon] : host;
    }

    /// <summary>Refuses any request whose authority is not the approved host, even if an absolute Uri overrides BaseAddress.</summary>
    private static SocketsHttpHandler CreateHostLockingHandler(string host)
    {
        return new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                if (!string.Equals(context.DnsEndPoint.Host, host, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The network gate only permits the host it approved.");
                var socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(new DnsEndPoint(context.DnsEndPoint.Host, context.DnsEndPoint.Port), cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };
    }
}
