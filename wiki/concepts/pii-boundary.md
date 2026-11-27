---
checked:
  docs/adr/0005-pii-minimize-mask-encrypt-ai-boundary.md: b071a1a1f40b6d0aa214dd317a314873c9213604
  docs/adr/0010-pii-masking-before-import.md: 99d7139b39965aef01e0ff0e94963e7b5e5db733
  OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs: 10ac66ed94a52da92f59d8a9355792bf36828413
  OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs: 274d180fd385ea73989c33d0f8f0312620adf476
  OfflineCPA/src/Backend/Endpoints/UploadEndpoints.cs: 404f93eb7df6a91fd45c0087b80953bf0b8c3e9f
  OfflineCPA/src/Backend/Extensions/SecureErrorHandling.cs: e57f61396acc5f925a3b7e45d170ed977a741596
  OfflineCPA/tests/TrueBalance.Tests/SecureErrorHandlingTests.cs: ed608ef9ce658ce40b4dc7aecd865e96795ce413
  OfflineCPA/src/Application/Ledger/DomainEventDispatcher.cs: 493387e8d82edf09427ea1a44e1f4883283fcb2c
---
# PII boundary

ADR 0005 (accepted) is the policy: minimize, mask, and encrypt personal data, and never send raw PII to an AI model (repo: docs/adr/0005-pii-minimize-mask-encrypt-ai-boundary.md#L9-L16). ADR 0010 (accepted) puts a masking core, prompt 04b, before statement import (repo: docs/adr/0010-pii-masking-before-import.md#L10-L11).

## What the code does today

The masking core has landed: domain `PiiPolicy` behind the `IPiiMasker` port, four local detectors, non-destructive tokens (last four digits plus an HMAC fingerprint), masked console logs, and masked upload error bodies. See [[pii-masking]].

The upload store writes a generated file name and keeps the client name off the path (repo: OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs#L52-L66). The folder watcher logs `{FileId}`, not the file name (repo: OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs#L58-L61), and its transaction stores the generated id, not the name or path (repo: OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs#L74-L80). The upload transaction does the same, with the display name masked (repo: OfflineCPA/src/Backend/Endpoints/UploadEndpoints.cs#L53-L58). Production error handling registers ProblemDetails and `UseExceptionHandler` (repo: OfflineCPA/src/Backend/Extensions/SecureErrorHandling.cs#L5-L14); the test asserts an unhandled exception body has no stack trace (repo: OfflineCPA/tests/TrueBalance.Tests/SecureErrorHandlingTests.cs#L15). Ledger audit summaries are the fixed strings in the dispatcher, not the memo (repo: OfflineCPA/src/Application/Ledger/DomainEventDispatcher.cs#L24-L29). The Excel export is a view over the database, which now holds only masked text and vault ids.

## Left for 06 (planned)

- Encryption at rest and the document vault: uploaded files are still stored unencrypted.
- Card, date-of-birth, and street-address detectors extend the same fixtures and module.
- Vendor TIN capture for 1099s, and the full no-leak suite.

Coverage, false positives and negatives are documented with citations in docs/privacy/masking-coverage.md.

## Change it safely

Do not add a real statement, a real tax id, or a real account number to fixtures. Contributors must not run real statements before 06 merges (repo: docs/adr/0010-pii-masking-before-import.md#L17). Update this page and [[pii-masking]] in the same PR as any detector or route change.
