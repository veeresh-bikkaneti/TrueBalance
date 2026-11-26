using Application.Ledger;
using Application.Networking;
using Infrastructure.Networking;
using TrueBalance.Tests.Ledger;

namespace TrueBalance.Tests.Networking;

/// <summary>Gate behavior: default deny, every attempt logged, host locked.</summary>
public class NetworkGateTests
{
    private static (LocalNetworkGate Gate, InMemoryLedgerStore Store) MakeGate(
        IIntegrationSettings? settings = null, FakeClock? clock = null)
    {
        var store = new InMemoryLedgerStore();
        var gate = new LocalNetworkGate(
            settings ?? new DisabledIntegrationSettings(),
            new LedgerNetworkAttemptLog(store),
            clock ?? new FakeClock());
        return (gate, store);
    }

    [Fact]
    public async Task Default_settings_deny_every_request_and_log_it()
    {
        var (gate, store) = MakeGate();

        var client = await gate.TryOpenClientAsync(new NetworkGateRequest("sample-adapter", "api.example.test"));

        Assert.Null(client);
        var row = Assert.Single(store.AuditRecords);
        Assert.Equal("NetworkGateDenied", row.EventType);
        Assert.Equal("network denied: sample-adapter api.example.test", row.Details);
        Assert.Equal("local", row.LocalUserId);
    }

    [Fact]
    public async Task Enabled_integration_gets_a_client_and_a_granted_row()
    {
        var (gate, store) = MakeGate(new EnabledIntegrationSettings());

        var client = await gate.TryOpenClientAsync(new NetworkGateRequest("sample-adapter", "api.example.test"));

        Assert.NotNull(client);
        Assert.Equal("https://api.example.test/", client!.BaseAddress!.ToString());
        var row = Assert.Single(store.AuditRecords);
        Assert.Equal("NetworkGateGranted", row.EventType);
        Assert.Equal("network granted: sample-adapter api.example.test", row.Details);
    }

    [Fact]
    public async Task Granted_client_with_a_port_keeps_the_port_in_the_base_address()
    {
        var (gate, store) = MakeGate(new EnabledIntegrationSettings());

        var client = await gate.TryOpenClientAsync(new NetworkGateRequest("sample-adapter", "api.example.test:8443"));

        Assert.NotNull(client);
        Assert.Equal("https://api.example.test:8443/", client!.BaseAddress!.ToString());
        var row = Assert.Single(store.AuditRecords);
        Assert.Equal("network granted: sample-adapter api.example.test:8443", row.Details);
    }

    [Fact]
    public async Task Unrelated_adapter_stays_denied_when_another_integration_is_enabled()
    {
        var (gate, store) = MakeGate(new EnabledIntegrationSettings());

        var client = await gate.TryOpenClientAsync(new NetworkGateRequest("other-adapter", "api.example.test"));

        Assert.Null(client);
        var row = Assert.Single(store.AuditRecords);
        Assert.Equal("NetworkGateDenied", row.EventType);
    }

    [Fact]
    public async Task Malformed_host_is_denied_and_logged_with_sanitized_values()
    {
        var (gate, store) = MakeGate(new EnabledIntegrationSettings());

        var client = await gate.TryOpenClientAsync(new NetworkGateRequest("sample-adapter", "api.example.test/../inject?user=1"));

        Assert.Null(client);
        var row = Assert.Single(store.AuditRecords);
        Assert.Equal("NetworkGateDenied", row.EventType);
        Assert.StartsWith("network denied: sample-adapter api.example.test", row.Details, StringComparison.Ordinal);
        Assert.DoesNotContain("/", row.Details, StringComparison.Ordinal);
        Assert.DoesNotContain("?", row.Details, StringComparison.Ordinal);
        Assert.DoesNotContain("=", row.Details, StringComparison.Ordinal);
        Assert.DoesNotContain("\r", row.Details, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", row.Details, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Empty_adapter_and_host_are_denied_and_logged_as_unknown_and_invalid()
    {
        var (gate, store) = MakeGate();

        var client = await gate.TryOpenClientAsync(new NetworkGateRequest("", ""));

        Assert.Null(client);
        var row = Assert.Single(store.AuditRecords);
        Assert.Equal("network denied: unknown invalid", row.Details);
    }

    private sealed class EnabledIntegrationSettings : IIntegrationSettings
    {
        public Task<bool> IsEnabledAsync(string adapterName, CancellationToken cancellationToken = default)
            => Task.FromResult(adapterName == "sample-adapter");
    }
}
