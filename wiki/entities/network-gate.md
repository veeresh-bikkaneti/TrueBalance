---
checked:
  OfflineCPA/src/Application/Networking/INetworkGate.cs: 77fab7a5d681fe7d25f236857a96c4ab8fe25914
  OfflineCPA/src/Infrastructure/Networking/LocalNetworkGate.cs: 9b776c957e947fa9159fe6cc21371dfa74ae1058
  OfflineCPA/src/Infrastructure/Networking/DisabledIntegrationSettings.cs: bcfebc42ced358fbc1f2fd78bc986a04e2812e0e
  OfflineCPA/src/Infrastructure/Networking/NetworkingModule.cs: 2e894beaff7eb7c1abd88d1b0a4abca493cf41aa
  OfflineCPA/src/Application/Ledger/LedgerNetworkAttemptLog.cs: 03a17539428b4a0688487187c90809a791b87335
  OfflineCPA/src/Application/Ai/IAiAssist.cs: b32faecea493216c82c0db7e88de674f29554945
  OfflineCPA/src/Infrastructure/AdapterModuleRegistration.cs: 98f6f27b02c94a2298b2860b84e40a66d8f1b56c
  OfflineCPA/src/Infrastructure/IAdapterModule.cs: d8fef4533ad154804d873af75694525f05154305
  OfflineCPA/src/Backend/Program.cs: e02af47f1e75ea8f35e7446d8b8315be41b80bc8
  OfflineCPA/src/Domain/Ledger/LedgerEvents.cs: dbf804ef2d845a43757fb28b13e0765a4e0cb54e
  OfflineCPA/src/Domain/Enums/LedgerAction.cs: b171c1e27fcdf7c9af5c2770a95219939dfcba3e
  OfflineCPA/tests/TrueBalance.Tests/Ledger/LedgerArchitectureTests.cs: 13d5b9f7311ccf67810976ba90a837f0a68c0f20
  OfflineCPA/tests/TrueBalance.Tests/Networking/NetworkGateTests.cs: 9462be3d36fd4af7a920053bc0c86173e91c2ad1
  OfflineCPA/tests/TrueBalance.Tests/Networking/NetworkSentinelTests.cs: a71d4dd2bfa13be81bda285c7f038ed3e95163c6
  OfflineCPA/tests/TrueBalance.Tests/Networking/AdapterModuleTests.cs: aeaa86e7e8433b80e021aa87031c886c3d42384a
---
# Network gate

`INetworkGate` is the port every outbound HTTP call must pass (repo: OfflineCPA/src/Application/Networking/INetworkGate.cs#L10-L21). `LocalNetworkGate` is the only adapter. The default is deny: no client is handed out, and the attempt is still logged (repo: OfflineCPA/src/Infrastructure/Networking/LocalNetworkGate.cs#L34-L56).

## Rules

- The adapter name and host are trimmed and lowercased; the host must match a host-only pattern, so paths, queries, and user info are refused before any decision (repo: OfflineCPA/src/Infrastructure/Networking/LocalNetworkGate.cs#L18-L42).
- `DisabledIntegrationSettings` is the shipped settings source: every integration stays off until the user's own settings say otherwise (repo: OfflineCPA/src/Infrastructure/Networking/DisabledIntegrationSettings.cs#L8-L14).
- When enabled, the gate returns an `HttpClient` whose handler refuses any request for a host other than the approved one (repo: OfflineCPA/src/Infrastructure/Networking/LocalNetworkGate.cs#L52-L93).
- Each attempt writes one `LedgerAuditRecords` row with `NetworkGateDenied` or `NetworkGateGranted` and a masked summary; the two name fields keep only lowercase letters, digits, dots, dashes, and colons (repo: OfflineCPA/src/Application/Ledger/LedgerNetworkAttemptLog.cs#L25-L45) (repo: OfflineCPA/src/Domain/Ledger/LedgerEvents.cs#L3-L10) (repo: OfflineCPA/src/Domain/Enums/LedgerAction.cs#L3-L11).
- `IAiAssist` is the AI port. No implementation is registered, and any future one receives tokenized text from the PII layer, never raw values (repo: OfflineCPA/src/Application/Ai/IAiAssist.cs#L5-L18).

## Adapter registration

Each adapter folder in Infrastructure has one `IAdapterModule` class; `AddInfrastructureAdapterModules` finds them by reflection and registers their adapters, so adding one needs no core edit (repo: OfflineCPA/src/Infrastructure/IAdapterModule.cs#L3-L12) (repo: OfflineCPA/src/Infrastructure/AdapterModuleRegistration.cs#L8-L22). The host calls it once (repo: OfflineCPA/src/Backend/Program.cs#L35-L38). The module list: `Infrastructure.Data`, `Infrastructure.Services`, `Infrastructure.Networking`, `Infrastructure.Reporting` (repo: OfflineCPA/src/Infrastructure/Networking/NetworkingModule.cs#L8-L15).

## Tests

`NetworkGateTests` covers default deny, granted path, per-adapter enablement, and sanitized summaries (repo: OfflineCPA/tests/TrueBalance.Tests/Networking/NetworkGateTests.cs#L11-L77). `AdapterModuleTests` asserts the scan registers each port and registers no `IAiAssist` (repo: OfflineCPA/tests/TrueBalance.Tests/Networking/AdapterModuleTests.cs#L10-L59). `LedgerArchitectureTests.Only_networking_modules_reference_http_or_sockets` fails if any type outside `Application.Networking` and `Infrastructure.Networking` touches `System.Net.Http` or `System.Net.Sockets` (repo: OfflineCPA/tests/TrueBalance.Tests/Ledger/LedgerArchitectureTests.cs#L45-L70). `NetworkSentinelTests` runs the main flows while sampling `/proc/net/tcp` and `/proc/net/tcp6` and fails naming any non-loopback endpoint that appears (repo: OfflineCPA/tests/TrueBalance.Tests/Networking/NetworkSentinelTests.cs#L20-L60).

## Change it safely

A new integration goes in its own adapter folder with its module class; it calls `TryOpenClientAsync` with its name and host and never builds its own `HttpClient`. Gate rows are append-only like the rest of [[event-log]]. See [[backend-host]] and [[uploads]].
