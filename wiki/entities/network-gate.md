---
checked:
  OfflineCPA/src/Application/Networking/INetworkGate.cs: 77fab7a5d681fe7d25f236857a96c4ab8fe25914
  OfflineCPA/src/Infrastructure/Networking/LocalNetworkGate.cs: 9b776c957e947fa9159fe6cc21371dfa74ae1058
  OfflineCPA/src/Infrastructure/Networking/DisabledIntegrationSettings.cs: bcfebc42ced358fbc1f2fd78bc986a04e2812e0e
  OfflineCPA/src/Infrastructure/Networking/NetworkingModule.cs: 6b71a6d1751dd5e52d395f2e9084f4af2ad754bd
  OfflineCPA/src/Infrastructure/Networking/ScopedNetworkAttemptLog.cs: 89c699f17af6648406dd0896dc1907cf71a05b0d
  OfflineCPA/src/Application/Ledger/LedgerNetworkAttemptLog.cs: 03a17539428b4a0688487187c90809a791b87335
  OfflineCPA/src/Application/Ai/IAiAssist.cs: b32faecea493216c82c0db7e88de674f29554945
  OfflineCPA/src/Infrastructure/AdapterModuleRegistration.cs: 98f6f27b02c94a2298b2860b84e40a66d8f1b56c
  OfflineCPA/src/Infrastructure/IAdapterModule.cs: d8fef4533ad154804d873af75694525f05154305
  OfflineCPA/src/Backend/Program.cs: e02af47f1e75ea8f35e7446d8b8315be41b80bc8
  OfflineCPA/src/Domain/Ledger/LedgerEvents.cs: dbf804ef2d845a43757fb28b13e0765a4e0cb54e
  OfflineCPA/src/Domain/Enums/LedgerAction.cs: b171c1e27fcdf7c9af5c2770a95219939dfcba3e
  OfflineCPA/tests/TrueBalance.Tests/Ledger/LedgerArchitectureTests.cs: c241452235c4fd1d5dbe64d134c98008fc5cf610
  OfflineCPA/tests/TrueBalance.Tests/Ledger/TypeNameHelper.cs: f04a70cc2a904c6be3bc1426315b5f50a10bf8d4
  OfflineCPA/tests/TrueBalance.Tests/Ledger/TypeNameHelperTests.cs: 6670dbca3b10d74c40588d8ce761a84a9e0642ad
  OfflineCPA/tests/TrueBalance.Tests/Networking/NetworkGateTests.cs: 9462be3d36fd4af7a920053bc0c86173e91c2ad1
  OfflineCPA/tests/TrueBalance.Tests/Networking/NetworkSentinelTests.cs: 28f0db4dd2ed13af5dd89b4d254710f06064486c
  OfflineCPA/tests/TrueBalance.Tests/Networking/GateLogIsolationTests.cs: 6fc0135e933a05e783edb15705f3b48891f0e462
  OfflineCPA/tests/TrueBalance.Tests/Networking/AdapterModuleTests.cs: aeaa86e7e8433b80e021aa87031c886c3d42384a
---
# Network gate

`INetworkGate` is the port every outbound HTTP call must pass (repo: OfflineCPA/src/Application/Networking/INetworkGate.cs#L10-L21). `LocalNetworkGate` is the only adapter. The default is deny: no client is handed out, and the attempt is still logged (repo: OfflineCPA/src/Infrastructure/Networking/LocalNetworkGate.cs#L34-L56).

## Rules

- The adapter name and host are trimmed and lowercased; the host must match a host-only pattern, so paths, queries, and user info are refused before any decision (repo: OfflineCPA/src/Infrastructure/Networking/LocalNetworkGate.cs#L18-L42).
- `DisabledIntegrationSettings` is the shipped settings source: every integration stays off until the user's own settings say otherwise (repo: OfflineCPA/src/Infrastructure/Networking/DisabledIntegrationSettings.cs#L8-L14).
- When enabled, the gate returns an `HttpClient` whose handler refuses any request for a host other than the approved one (repo: OfflineCPA/src/Infrastructure/Networking/LocalNetworkGate.cs#L52-L93).
- Each attempt writes one `LedgerAuditRecords` row with `NetworkGateDenied` or `NetworkGateGranted` and a masked summary; the two name fields keep only lowercase letters, digits, dots, dashes, and colons (repo: OfflineCPA/src/Application/Ledger/LedgerNetworkAttemptLog.cs#L25-L45) (repo: OfflineCPA/src/Domain/Ledger/LedgerEvents.cs#L3-L10) (repo: OfflineCPA/src/Domain/Enums/LedgerAction.cs#L3-L11).
- The wired log is the `ScopedNetworkAttemptLog` decorator: it opens a fresh scope per attempt, so the gate row is the only change that scope saves and a gate call made mid-use-case cannot commit the caller's pending work (repo: OfflineCPA/src/Infrastructure/Networking/ScopedNetworkAttemptLog.cs#L13-L27) (repo: OfflineCPA/src/Infrastructure/Networking/NetworkingModule.cs#L12-L15).
- `IAiAssist` is the AI port. No implementation is registered, and any future one receives tokenized text from the PII layer, never raw values (repo: OfflineCPA/src/Application/Ai/IAiAssist.cs#L5-L18).

## Adapter registration

Each adapter folder in Infrastructure has one `IAdapterModule` class; `AddInfrastructureAdapterModules` finds them by reflection and registers their adapters, so adding one needs no core edit (repo: OfflineCPA/src/Infrastructure/IAdapterModule.cs#L3-L12) (repo: OfflineCPA/src/Infrastructure/AdapterModuleRegistration.cs#L8-L22). The host calls it once (repo: OfflineCPA/src/Backend/Program.cs#L35-L38). The module list: `Infrastructure.Data`, `Infrastructure.Services`, `Infrastructure.Networking`, `Infrastructure.Reporting` (repo: OfflineCPA/src/Infrastructure/Networking/NetworkingModule.cs#L8-L15).

## Tests

`NetworkGateTests` covers default deny, granted path, per-adapter enablement, and sanitized summaries (repo: OfflineCPA/tests/TrueBalance.Tests/Networking/NetworkGateTests.cs#L11-L77). `AdapterModuleTests` asserts the scan registers each port and registers no `IAiAssist` (repo: OfflineCPA/tests/TrueBalance.Tests/Networking/AdapterModuleTests.cs#L10-L59). `GateLogIsolationTests` proves the gate row commits in its own scope while the caller's pending row stays pending (repo: OfflineCPA/tests/TrueBalance.Tests/Networking/GateLogIsolationTests.cs#L17-L38).

`LedgerArchitectureTests.Only_networking_modules_reference_http_or_sockets` fails if any type outside `Application.Networking` and `Infrastructure.Networking` touches the blocked families: `System.Net.Http`, `System.Net.Sockets`, `System.Net.WebSockets`, `System.Net.Mail`, `System.Net.WebClient`, `System.Net.WebRequest`, `System.Net.HttpWebRequest`, and `System.Net.Dns` (repo: OfflineCPA/tests/TrueBalance.Tests/Ledger/LedgerArchitectureTests.cs#L51-L88). NetArchTest matches a dependency name as a dotted segment chain of the referenced type's full name, so each name covers its whole namespace or type (source: https://github.com/BenMorris/NetArchTest, src/NetArchTest.Rules/Dependencies/DataStructures/NamespaceTree.cs, GetAllMatchingNames). [Confidence: High] Nested failing names map to their outer type through `TypeNameHelper`, so a nested type can no longer slip past the rule (repo: OfflineCPA/tests/TrueBalance.Tests/Ledger/TypeNameHelper.cs#L9-L15) (repo: OfflineCPA/tests/TrueBalance.Tests/Ledger/TypeNameHelperTests.cs#L5-L31). A probe test exercises both mechanics against a deliberate `System.Net.Dns` probe in the test assembly (repo: OfflineCPA/tests/TrueBalance.Tests/Ledger/LedgerArchitectureTests.cs#L90-L103). `NetworkSentinelTests` runs the main flows while sampling `/proc/net/tcp` and `/proc/net/tcp6` and fails naming any non-loopback endpoint that appears on a socket this process owns; socket inodes come from `/proc/self/fd`, so other processes' connections cannot fail it (repo: OfflineCPA/tests/TrueBalance.Tests/Networking/NetworkSentinelTests.cs#L21-L66).

## Change it safely

A new integration goes in its own adapter folder with its module class; it calls `TryOpenClientAsync` with its name and host and never builds its own `HttpClient`. Gate rows are append-only like the rest of [[event-log]]. See [[backend-host]] and [[uploads]].
