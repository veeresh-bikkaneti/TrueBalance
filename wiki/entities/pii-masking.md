---
checked:
  docs/adr/0010-pii-masking-before-import.md: 99d7139b39965aef01e0ff0e94963e7b5e5db733
  OfflineCPA/src/Domain/Pii/PiiPolicy.cs: 0c7cd9b78d7508894224fc264904319ea15e1fbd
  OfflineCPA/src/Application/Interfaces/IPiiMasker.cs: c5a61065c626451d907e20e5f30172d52478ddaf
  OfflineCPA/src/Infrastructure/Pii/PiiMasker.cs: 1993c33130f9d60491b892564a41c4b580a815ae
  OfflineCPA/src/Infrastructure/Pii/LocalFingerprintKeyStore.cs: f076fdf4bc6c88f1687eb84629810a1df38f3e8f
  OfflineCPA/src/Infrastructure/Pii/PiiModule.cs: 8c690cff4a66ea442f07857c106683aeb449110f
  OfflineCPA/src/Infrastructure/Pii/Detectors/SsnDetector.cs: fe03d1e2dcae78a9b3bcd95f1973d375f119d26e
  OfflineCPA/src/Infrastructure/Pii/Detectors/EinItinDetector.cs: 0cedc4add8329e2b781dc890220accdf04a56795
  OfflineCPA/src/Infrastructure/Pii/Detectors/AbaRoutingNumberDetector.cs: dfae3e5e7d411e491d9ef1776992eb49aa0b1340
  OfflineCPA/src/Infrastructure/Pii/Detectors/BankAccountNumberDetector.cs: 81754c2404b0dc0bfa559b943c59f28e288dca79
  OfflineCPA/src/Backend/Extensions/PiiRedactionLoggerProvider.cs: 101548e1a99b7f0447839b194dac157eb143e084
  OfflineCPA/src/Backend/Program.cs: 17e6fc6fe3d31caa311ea9bc4bb5f99e074abfdd
  OfflineCPA/src/Backend/Endpoints/UploadEndpoints.cs: 404f93eb7df6a91fd45c0087b80953bf0b8c3e9f
  OfflineCPA/tests/TrueBalance.Tests/Pii/Fixtures/FakePii.cs: bf6f6a73fd062d9d66aa16b08fff6459511d1940
  OfflineCPA/tests/TrueBalance.Tests/Pii/PiiNoLeakTests.cs: edb514c01e5710ed46cf200dd8c27349628a96c4
---
# PII masking core

ADR 0010 (accepted) requires masking before any database or log write on the import path (repo: docs/adr/0010-pii-masking-before-import.md#L13-L20). This module is that core, shipped by prompt 04b.

## Layers

The domain owns the policy: `PiiPolicy` takes a local HMAC key plus the detectors and replaces each detected value with a token holding the last four digits and a fingerprint prefix (repo: OfflineCPA/src/Domain/Pii/PiiPolicy.cs#L48-L93). Overlapping matches resolve by type priority first, then position: routing (check digit), SSN, EIN/ITIN, then bank account (repo: OfflineCPA/src/Domain/Pii/PiiPolicy.cs#L53-L62). The fingerprint is HMAC-SHA256 over the digits only, so separator variants of one number share a digest (repo: OfflineCPA/src/Domain/Pii/PiiPolicy.cs#L104-L110).

The port is narrow: callers pass text through `IPiiMasker.Mask`, or `MaskDetailed` when they store structured matching keys (prompt 05), and hold no detection logic (repo: OfflineCPA/src/Application/Interfaces/IPiiMasker.cs#L6-L16). The adapter takes the key from `Pii:FingerprintKey` (base64) when set, otherwise generates 32 bytes once and persists them to `Pii:FingerprintKeyPath` (default `data/pii-fingerprint.key`, anchored to the app content root by a post-configure step and git-ignored via `**/pii-fingerprint.key`), so fingerprints survive restarts (repo: OfflineCPA/src/Infrastructure/Pii/PiiMasker.cs#L15-L46) (repo: OfflineCPA/src/Infrastructure/Pii/LocalFingerprintKeyStore.cs#L13-L57).

The detectors are one class per type and register through the `PiiModule` adapter module, so prompt 06 adds detectors without touching the host (repo: OfflineCPA/src/Infrastructure/Pii/PiiModule.cs#L10-L20): SSN with invalid-range skips (repo: OfflineCPA/src/Infrastructure/Pii/Detectors/SsnDetector.cs#L12-L53), EIN plus ITIN middle-digit ranges (repo: OfflineCPA/src/Infrastructure/Pii/Detectors/EinItinDetector.cs#L13-L56), ABA routing by check digit (repo: OfflineCPA/src/Infrastructure/Pii/Detectors/AbaRoutingNumberDetector.cs#L12-L40), and 8-17 digit bank account runs with phone, date, amount, and tax-id-shape exclusions (repo: OfflineCPA/src/Infrastructure/Pii/Detectors/BankAccountNumberDetector.cs#L17-L130).

## Logs and errors

Every console log line is masked before the provider writes it; every message on the exception chain is checked, and a chain that changes under masking is rebuilt from type names and masked messages (repo: OfflineCPA/src/Backend/Extensions/PiiRedactionLoggerProvider.cs#L14-L132). `Program.cs` clears the default providers and wraps the console provider (repo: OfflineCPA/src/Backend/Program.cs#L24-L30). Upload errors quote input, so the route masks the message before returning it (repo: OfflineCPA/src/Backend/Endpoints/UploadEndpoints.cs#L44-L45).

## Tests and fixtures

Coverage, known false positives and negatives, and the contributor warning are in docs/privacy/masking-coverage.md. Synthetic fixture values live in one shared folder so 06 extends them (repo: OfflineCPA/tests/TrueBalance.Tests/Pii/Fixtures/FakePii.cs#L11-L44). The no-leak gate: fake values pass through the masker and a stub importer path, then the test asserts none of them appear in any database text column, captured logs, or an error body (repo: OfflineCPA/tests/TrueBalance.Tests/Pii/PiiNoLeakTests.cs#L22-L190). Statement import (prompt 05) cannot merge until it passes.

## Change it safely

Do not add a detector with a network call or an AI hook; local patterns only. Do not put a real or realistic SSN, EIN, ITIN, routing, or account number in a fixture, log line, or wiki page. Keep [[pii-boundary]] and docs/privacy/masking-coverage.md in sync with every detector change.
