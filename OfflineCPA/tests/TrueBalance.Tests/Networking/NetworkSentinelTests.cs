using Application.Ledger;
using Domain.Entities;
using TrueBalance.Tests.Ledger;

namespace TrueBalance.Tests.Networking;

/// <summary>
/// Network sentinel: while the main flows run, this test process must not
/// open a socket to a non-loopback address. On Linux (CI) the harness watches
/// /proc/net/tcp and /proc/net/tcp6 but only counts rows whose inode belongs
/// to a socket owned by this process (collected from /proc/self/fd), so other
/// processes' connections cannot fail the test. Loopback is still allowed
/// because the database itself may listen there.
/// On other systems the part after the format check cannot run; the
/// architecture test <see cref="TrueBalance.Tests.Ledger.LedgerArchitectureTests.Only_networking_modules_reference_http_or_sockets"/>
/// covers the type-level rule instead.
/// </summary>
public class NetworkSentinelTests
{
    [Fact]
    public async Task Main_flows_open_no_network_sockets()
    {
        if (!File.Exists("/proc/net/tcp"))
            return; // Windows and macOS have no /proc; CI is the source of truth here.

        // The host (CI agent, IDE, database) may already hold loopback or
        // long-lived sockets; those were not opened by the flows. The sentinel
        // fails only on a non-loopback endpoint that appears during the run.
        var preExisting = new HashSet<string>();
        var ownInodes = ReadOwnSocketInodes();
        foreach (var path in new[] { "/proc/net/tcp", "/proc/net/tcp6" })
            foreach (var socket in ReadNonLoopbackSockets(path, ownInodes))
                preExisting.Add(socket.Remote);

        var violations = new List<string>();
        using var cts = new CancellationTokenSource();
        var watchdog = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                foreach (var path in new[] { "/proc/net/tcp", "/proc/net/tcp6" })
                    foreach (var socket in ReadNonLoopbackSockets(path, ownInodes))
                        if (!preExisting.Contains(socket.Remote) || socket.State == "02")
                            violations.Add($"{path}: remote {socket.Remote} state {socket.State}");
                try { await Task.Delay(25, cts.Token); }
                catch (TaskCanceledException) { }
            }
        });

        try
        {
            await RunMainFlowsAsync();
        }
        finally
        {
            await cts.CancelAsync();
            await Task.WhenAny(watchdog);
        }

        // Give the sampler a last chance to catch something that opened and closed.
        foreach (var path in new[] { "/proc/net/tcp", "/proc/net/tcp6" })
            foreach (var socket in ReadNonLoopbackSockets(path, ReadOwnSocketInodes()))
                if (!preExisting.Contains(socket.Remote) || socket.State == "02")
                    violations.Add($"{path}: remote {socket.Remote} state {socket.State}");

        Assert.True(violations.Count == 0,
            "A socket other than the local DB opened during the main flows: " + string.Join(", ", violations));
    }

    /// <summary>The main flows: draft an entry, post it, read balances, and log a gate denial.</summary>
    private static async Task RunMainFlowsAsync()
    {
        var store = new InMemoryLedgerStore();
        var clock = new FakeClock();
        var period = LedgerTestData.YearPeriod();
        store.AddPeriod(period);
        foreach (var account in LedgerTestData.FiveAccounts())
            store.AddAccount(account);

        var create = new CreateDraftEntry(store, clock);
        var post = new PostEntry(store, clock);
        var balances = new GetAccountBalances(store);
        var gateLog = new LedgerNetworkAttemptLog(store);

        var id = await create.HandleAsync(new CreateDraftEntryCommand(
            LedgerTestData.Day, "Fake Vendor 1 purchase", period.Id,
            new List<DraftPostingLine>
            {
                new(LedgerTestData.Expense().Id, 100m, null),
                new(LedgerTestData.Checking().Id, -100m, null)
            }, "local"));
        await post.HandleAsync(new PostEntryCommand(id, "local"));
        Assert.NotEmpty(await balances.HandleAsync());

        var gate = new Infrastructure.Networking.LocalNetworkGate(
            new Infrastructure.Networking.DisabledIntegrationSettings(), gateLog, clock);
        Assert.Null(await gate.TryOpenClientAsync(new Application.Networking.NetworkGateRequest("sample-adapter", "api.example.test")));
        Assert.Single(store.AuditRecords, r => r.EventType == "NetworkGateDenied");
        Assert.Single(store.AuditRecords, r => r.EventType == "EntryDrafted");
        Assert.Single(store.AuditRecords, r => r.EventType == "EntryPosted");
    }

    /// <summary>A remote endpoint observed in an active TCP state.</summary>
    private sealed record SocketEndpoint(string Remote, string State);

    /// <summary>The socket inodes this process owns, read from /proc/self/fd links like "socket:[12345]".</summary>
    private static IReadOnlySet<string> ReadOwnSocketInodes()
    {
        try
        {
            var inodes = new HashSet<string>();
            foreach (var entry in Directory.EnumerateFileSystemEntries("/proc/self/fd"))
            {
                var target = TryReadLink(entry);
                // Format: "socket:[12345]". The inode is the digits inside the brackets.
                const string prefix = "socket:[";
                if (target is not null && target.StartsWith(prefix, StringComparison.Ordinal) && target.EndsWith("]", StringComparison.Ordinal))
                    inodes.Add(target[prefix.Length..^1]);
            }

            return inodes;
        }
        catch (IOException)
        {
            return new HashSet<string>();
        }
    }

    private static string? TryReadLink(string path)
    {
        try
        {
            return File.ResolveLinkTarget(path, returnFinalTarget: false)?.FullName;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Returns ESTABLISHED (01) or SYN_SENT (02) sockets that are not loopback and belong to this process.</summary>
    private static IEnumerable<SocketEndpoint> ReadNonLoopbackSockets(string path, IReadOnlySet<string> ownInodes)
    {
        try
        {
            if (!File.Exists(path))
                return Array.Empty<SocketEndpoint>();

            var offending = new List<SocketEndpoint>();
            foreach (var line in File.ReadLines(path).Skip(1))
            {
                var columns = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length < 4)
                    continue;

                // Column layout: sl local_address rem_address st tx:rx tr retrnsmt uid timeout inode.
                var inode = columns.Length > 9 ? columns[9] : string.Empty;
                if (ownInodes.Count != 0 && !ownInodes.Contains(inode))
                    continue;

                var remote = columns[2];
                var state = columns[3];
                if (state is not ("01" or "02"))
                    continue;
                if (IsLoopback(remote))
                    continue;

                offending.Add(new SocketEndpoint(remote, state));
            }

            return offending;
        }
        catch (IOException)
        {
            return Array.Empty<SocketEndpoint>();
        }
    }

    /// <summary>IPv4 remotes 127.x.x.x and the IPv6 loopback ::1 are the local address family.</summary>
    private static bool IsLoopback(string remoteAddress)
    {
        var address = remoteAddress.Split(':')[0];
        if (address.Length == 32)
            return address == "00000000000000000000000000000001";
        return address.Length == 8 && address.StartsWith("7F", StringComparison.OrdinalIgnoreCase);
    }
}
