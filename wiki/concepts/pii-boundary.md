---
checked:
  docs/adr/0005-pii-minimize-mask-encrypt-ai-boundary.md: b071a1a1f40b6d0aa214dd317a314873c9213604
  docs/adr/0010-pii-masking-before-import.md: 99d7139b39965aef01e0ff0e94963e7b5e5db733
  OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs: 10ac66ed94a52da92f59d8a9355792bf36828413
  OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs: 87d4c2d8c4544c6a4102d6f1353215105cebfa8e
  OfflineCPA/src/Backend/Extensions/SecureErrorHandling.cs: e57f61396acc5f925a3b7e45d170ed977a741596
  OfflineCPA/tests/TrueBalance.Tests/SecureErrorHandlingTests.cs: ed608ef9ce658ce40b4dc7aecd865e96795ce413
  OfflineCPA/src/Application/Ledger/DomainEventDispatcher.cs: 493387e8d82edf09427ea1a44e1f4883283fcb2c
  OfflineCPA/src/Backend/Program.cs: e02af47f1e75ea8f35e7446d8b8315be41b80bc8
  docs/specs/prompts/04b-pii-masking-core.md: d2cd95b483354264bc3ad4fbdaa34a01f66c7f76
---
# PII boundary

ADR 0005 (accepted) is the policy: minimize, mask, and encrypt personal data, and never send raw PII to an AI model (repo: docs/adr/0005-pii-minimize-mask-encrypt-ai-boundary.md#L9-L16). ADR 0010 (accepted) puts a masking core, prompt 04b, before statement import. Prompt 06 adds encryption, the vault, and the rest of the detectors (repo: docs/adr/0010-pii-masking-before-import.md#L10-L11).

## What the code does today

The upload store writes a generated file name and keeps the client name off the path (repo: OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs#L52-L66). The folder watcher logs `{FileId}`, not the original name (repo: OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs#L58-L61). Production error handling registers ProblemDetails and `UseExceptionHandler` (repo: OfflineCPA/src/Backend/Extensions/SecureErrorHandling.cs#L5-L14). The test asserts an unhandled exception body has no stack trace (repo: OfflineCPA/tests/TrueBalance.Tests/SecureErrorHandlingTests.cs#L15). Ledger audit summaries are the fixed strings in the dispatcher, not the memo (repo: OfflineCPA/src/Application/Ledger/DomainEventDispatcher.cs#L24-L29).

The upload route still stores the original file name in `Transaction.Description` and the full stored path in `ReferenceDocumentPath` (repo: OfflineCPA/src/Backend/Program.cs#L106-L110). The folder watcher does the same with `Path.GetFileName` (repo: OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs#L72-L92). The Excel export writes that description and path (repo: OfflineCPA/src/Backend/Program.cs#L154-L160).

## Left for 04b (planned)

Prompt 04b, not this tree, specifies the work still to do (repo: docs/specs/prompts/04b-pii-masking-core.md#L14-L28):

- A domain `PiiPolicy` behind `IPiiMasker`. Detectors for bank account, ABA routing, SSN, and EIN/ITIN. Local patterns only.
- Keep the last 4 digits and an HMAC-SHA256 fingerprint. Do not store the full value.
- A log and error redaction filter, and a no-leak test on synthetic fixtures.
- Stop writing the original file name and the full path on the upload transaction. Store the generated id.
- Cards, dates of birth, street addresses, encryption, and the vault wait for prompt 06.

## Change it safely

Do not add a real statement, a real tax id, or a real account number to fixtures. When 04b lands, update this page and [[uploads]] in the same PR. Contributors must not run real statements before 06 merges (repo: docs/adr/0010-pii-masking-before-import.md#L17).
