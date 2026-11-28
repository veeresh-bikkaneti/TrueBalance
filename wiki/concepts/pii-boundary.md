---
checked:
  docs/adr/0005-pii-minimize-mask-encrypt-ai-boundary.md: b071a1a1f40b6d0aa214dd317a314873c9213604
  docs/adr/0010-pii-masking-before-import.md: 99d7139b39965aef01e0ff0e94963e7b5e5db733
  OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs: 25555efe955066ce7e89bb2ae445793cc60b8677
  OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs: 386a38d5ea989a34871a17c5bc3c26a2d4626304
  OfflineCPA/src/Backend/Endpoints/UploadEndpoints.cs: 8eebefad33f134ade84e8f918dda3c647dbe3be3
  OfflineCPA/src/Backend/Extensions/SecureErrorHandling.cs: e57f61396acc5f925a3b7e45d170ed977a741596
  OfflineCPA/tests/TrueBalance.Tests/SecureErrorHandlingTests.cs: ed608ef9ce658ce40b4dc7aecd865e96795ce413
  OfflineCPA/src/Application/Ledger/DomainEventDispatcher.cs: 493387e8d82edf09427ea1a44e1f4883283fcb2c
---
# PII boundary

ADR 0005 (accepted) is the policy: minimize, mask, and encrypt personal data, and never send raw PII to an AI model (repo: docs/adr/0005-pii-minimize-mask-encrypt-ai-boundary.md#L9-L16). ADR 0010 (accepted) puts a masking core, prompt 04b, before statement import (repo: docs/adr/0010-pii-masking-before-import.md#L10-L11).

## What the code does today

The masking core has landed: domain `PiiPolicy` behind the `IPiiMasker` port, four local detectors, non-destructive tokens (last four digits plus an HMAC fingerprint), masked console logs, and masked upload error bodies. See [[pii-masking]].

The vault seals every upload under a generated name (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L157-L207); see [[uploads]] for the key and unlock rules. Its files use a random data key that is stored only sealed, by a passphrase-derived key and, once a recovery code is made, by a code-derived key (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L94-L117), and the passphrase can change without encrypting the files again (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L73-L92) (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L348-L362). The folder watcher logs `{FileId}`, not the file name (repo: OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs#L66-L67). It stores each file in the vault and queues an import batch, so no name or path reaches the database (repo: OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs#L92-L112). The upload route queues the same way (repo: OfflineCPA/src/Backend/Endpoints/UploadEndpoints.cs#L50-L52). Staged rows hold masked descriptions; see [[statement-import]]. Production error handling registers ProblemDetails and `UseExceptionHandler` (repo: OfflineCPA/src/Backend/Extensions/SecureErrorHandling.cs#L5-L14); the test asserts an unhandled exception body has no stack trace (repo: OfflineCPA/tests/TrueBalance.Tests/SecureErrorHandlingTests.cs#L15). Ledger audit summaries are the fixed strings in the dispatcher, not the memo (repo: OfflineCPA/src/Application/Ledger/DomainEventDispatcher.cs#L24-L29). The Excel export is a view over the database, which now holds only masked text and vault ids.

## Left for 06 (planned)

- The field protector, masked-by-default exports and moving memo masking into the use case (prompt 08).
- Card, date-of-birth, and street-address detectors extend the same fixtures and module.
- Vendor TIN capture for 1099s, and the full no-leak suite.

Coverage, false positives and negatives are documented with citations in docs/privacy/masking-coverage.md.

## Change it safely

Do not add a real statement, a real tax id, or a real account number to fixtures. Contributors must not run real statements before 06 merges (repo: docs/adr/0010-pii-masking-before-import.md#L17). Update this page and [[pii-masking]] in the same PR as any detector or route change.
