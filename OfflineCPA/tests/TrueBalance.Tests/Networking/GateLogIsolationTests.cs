using Application.Interfaces;
using Application.Ledger;
using Application.Networking;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Networking;
using Microsoft.Extensions.DependencyInjection;
using TrueBalance.Tests.Ledger;

namespace TrueBalance.Tests.Networking;

/// <summary>
/// The gate's log write must not commit the caller's unit of work. A use case
/// with a pending, unsaved audit row calls the gate; afterwards the gate row
/// exists and the caller's row is still unsaved.
/// </summary>
public class GateLogIsolationTests
{
    private static readonly DateTime Now = new(2026, 11, 26, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Gate_log_write_commits_only_its_own_row()
    {
        var callerStore = new InMemoryLedgerStore();
        var gateStore = new InMemoryLedgerStore();

        // A use case in flight: its audit row is pending and unsaved.
        await callerStore.AppendAuditAsync(
            LedgerAuditRecord.For(LedgerAction.DraftCreated, "EntryDrafted", "local", Now, details: "draft created"));
        Assert.Empty(callerStore.AuditRecords);

        var gate = new LocalNetworkGate(
            new DisabledIntegrationSettings(),
            new ScopedNetworkAttemptLog(new StubScopeFactory(gateStore)),
            new FakeClock());
        Assert.Null(await gate.TryOpenClientAsync(new NetworkGateRequest("sample-adapter", "api.example.test")));

        // The gate row is committed in its own scope.
        Assert.Single(gateStore.AuditRecords, r => r.EventType == "NetworkGateDenied");
        Assert.Equal(1, gateStore.SaveCount);

        // The caller's pending row is still pending, and nothing saved the caller's unit of work.
        Assert.Empty(callerStore.AuditRecords);
        Assert.Equal(0, callerStore.SaveCount);
    }

    private sealed class StubScopeFactory : IServiceScopeFactory
    {
        private readonly ILedgerStore _store;
        public StubScopeFactory(ILedgerStore store) => _store = store;
        public IServiceScope CreateScope() => new StubScope(_store);
    }

    private sealed class StubScope : IServiceScope
    {
        public StubScope(ILedgerStore store) => ServiceProvider = new StubProvider(store);
        public IServiceProvider ServiceProvider { get; }
        public void Dispose() { }
    }

    private sealed class StubProvider : IServiceProvider
    {
        private readonly ILedgerStore _store;
        public StubProvider(ILedgerStore store) => _store = store;
        public object? GetService(Type serviceType) => serviceType == typeof(ILedgerStore) ? _store : null;
    }
}
