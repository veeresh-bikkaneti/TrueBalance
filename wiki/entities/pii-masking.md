---
checked:
  docs/adr/0010-pii-masking-before-import.md: 99d7139b39965aef01e0ff0e94963e7b5e5db733
  OfflineCPA/src/Domain/Pii/PiiPolicy.cs: b86194ed3255139fe53b42cbc7cd7ada6557f149
  OfflineCPA/src/Application/Interfaces/IPiiMasker.cs: c5a61065c626451d907e20e5f30172d52478ddaf
  OfflineCPA/src/Infrastructure/Pii/PiiMasker.cs: ec9afd578f6b17f61cdf1a4f35d6aefa1beaf75e
  OfflineCPA/src/Infrastructure/Pii/LocalFingerprintKeyStore.cs: 61d322ca1468d8c687bd5b828d19ff078ef52b1f
  OfflineCPA/src/Infrastructure/Pii/PiiModule.cs: 46bdf6555102ff58bf2241609ac04084c67bf2e9
  OfflineCPA/src/Infrastructure/Pii/Detectors/PiiDetectors.cs: 322da7c5bed306d343010cce9d0103d646bb9035
  OfflineCPA/src/Infrastructure/Pii/Detectors/SsnDetector.cs: 8173fe31c27dce7c81a307a66efb9d6b4dfa54b1
  OfflineCPA/src/Infrastructure/Pii/Detectors/EinItinDetector.cs: 010b52872b3fc4f2cc8064cb204f33b6d390bf07
  OfflineCPA/src/Infrastructure/Pii/Detectors/AbaRoutingNumberDetector.cs: 9c7f47fe4ea374f862ae466ca2c7b4a57742c962
  OfflineCPA/src/Infrastructure/Pii/Detectors/BankAccountNumberDetector.cs: dd2e304407ee28dc3aa71c0fb9b46605902d8404
  OfflineCPA/src/Infrastructure/Pii/Detectors/DateOfBirthDetector.cs: 9f286594f11e24b0448283cbf7f77586159489fe
  OfflineCPA/src/Infrastructure/Pii/Detectors/StreetAddressDetector.cs: bc456b3984b2a78a42eadf5adc5b730077bfce6c
  OfflineCPA/src/Infrastructure/Pii/Detectors/PhoneNumberDetector.cs: 42ff20a98eb4c2abda9baeca9d6cedde5b3fbb02
  OfflineCPA/src/Infrastructure/Pii/Detectors/EmailDetector.cs: 0d40ef03c2d9715aa534baf01fcb72cfe17a55f8
  OfflineCPA/src/Application/Ledger/CreateDraftEntry.cs: b40f48ed88f2f9d964860b83e41cb07207e54ffa
  OfflineCPA/src/Application/Ledger/ReverseEntry.cs: 805229d6fa35cd9101942801206aa721a7e39baa
  OfflineCPA/src/Backend/Endpoints/LedgerEndpoints.cs: 01685af3dfbc1a4547ff17a824dbafdc08d9afdd
  OfflineCPA/src/Backend/Extensions/PiiRedactionLoggerProvider.cs: 50ff20605e62fc05011c5f6a16392eeb23aeb600
  OfflineCPA/src/Backend/Program.cs: 742bc67ef2e99c040cdbdaba2b50a1fcda30bf82
  OfflineCPA/src/Backend/Endpoints/UploadEndpoints.cs: 8eebefad33f134ade84e8f918dda3c647dbe3be3
  OfflineCPA/tests/TrueBalance.Tests/Pii/Fixtures/FakePii.cs: 862083bc55b6f480ea7f19694bc08750f2eaacf4
  OfflineCPA/tests/TrueBalance.Tests/Pii/PiiNoLeakTests.cs: 847b2efdf07f975305c68bdf7c07c5e3c8ba0eb9
  OfflineCPA/tests/TrueBalance.Tests/Imports/ImportNoLeakTests.cs: ca30e071a437dffe0c14cac59d577daddc43ba12
---
# PII masking core

ADR 0010 (accepted) requires masking before any database or log write on the import path (repo: docs/adr/0010-pii-masking-before-import.md#L13-L20). This module is that core, shipped by prompt 04b.

## Layers

The domain owns the policy: `PiiPolicy` takes a local HMAC key plus the detectors and replaces each detected value with a token holding the last four digits and a fingerprint prefix (repo: OfflineCPA/src/Domain/Pii/PiiPolicy.cs#L63-L120). Overlapping matches resolve by type priority first, then position: email, routing (check digit), card, SSN, EIN/ITIN, date of birth and address, phone, then bank account (repo: OfflineCPA/src/Domain/Pii/PiiPolicy.cs#L20-L37). The fingerprint is HMAC-SHA256 over the digits only, so separator variants of one number share a digest (repo: OfflineCPA/src/Domain/Pii/PiiPolicy.cs#L183-L183).

The port is narrow: callers pass text through `IPiiMasker.Mask`, or `MaskDetailed` when they store structured matching keys (prompt 05), and hold no detection logic (repo: OfflineCPA/src/Application/Interfaces/IPiiMasker.cs#L6-L16). The adapter takes the key from `Pii:FingerprintKey` (base64) when set, otherwise generates 32 bytes once and persists them to `Pii:FingerprintKeyPath` (default `data/pii-fingerprint.key`, anchored to the app content root by a post-configure step and git-ignored via `**/pii-fingerprint.key`), so fingerprints survive restarts (repo: OfflineCPA/src/Infrastructure/Pii/PiiMasker.cs#L15-L50) (repo: OfflineCPA/src/Infrastructure/Pii/LocalFingerprintKeyStore.cs#L14-L89).

The detectors are one class per type and register through the `PiiModule` adapter module, so prompt 06 adds detectors without touching the host (repo: OfflineCPA/src/Infrastructure/Pii/PiiModule.cs#L9-L20). The set is listed once, in `PiiDetectors.All()`, which the app and the test masker both read (repo: OfflineCPA/src/Infrastructure/Pii/Detectors/PiiDetectors.cs#L9-L25): SSN with invalid-range skips (repo: OfflineCPA/src/Infrastructure/Pii/Detectors/SsnDetector.cs#L12-L53), EIN plus ITIN middle-digit ranges (repo: OfflineCPA/src/Infrastructure/Pii/Detectors/EinItinDetector.cs#L13-L56), ABA routing by check digit (repo: OfflineCPA/src/Infrastructure/Pii/Detectors/AbaRoutingNumberDetector.cs#L12-L40), and 8-17 digit bank account runs with phone, date, amount, and tax-id-shape exclusions (repo: OfflineCPA/src/Infrastructure/Pii/Detectors/BankAccountNumberDetector.cs#L17-L254).

Prompt 06 adds card numbers (Luhn-checked), dates of birth (only after a birth label such as `DOB`, `born` or `birth date`), and US street addresses with any unit, plus PO boxes. Dates and addresses keep no readable digits in their token (repo: OfflineCPA/src/Domain/Pii/PiiPolicy.cs#L39-L50). The three PR 45 gaps are closed in the account detector: unbroken runs of 18 or more digits, account groups joined to an SSN by an underscore (the run after its first separator is offered as a second candidate), and runs split by a double space, `--` or ` - ` (repo: OfflineCPA/src/Infrastructure/Pii/Detectors/BankAccountNumberDetector.cs#L44-L111). Space-grouped runs of eight or more digits are masked by default. A run stays readable only when a list label (`Invoice(s)`, `Inv`, `Inv #`, `Check(s)`, `Chk`, `Order(s)`) sits directly before it and every group is 3 to 6 digits; a card layout (4-4-4-4, 4-6-5, 4-6-4) is masked even then (repo: OfflineCPA/src/Infrastructure/Pii/Detectors/BankAccountNumberDetector.cs#L98-L111). Prompt 06c widened the shapes. Dates of birth take a dash, two-digit years and English month names (repo: OfflineCPA/src/Infrastructure/Pii/Detectors/DateOfBirthDetector.cs#L1-L42). Addresses take the USPS suffix list (without `C`, `Un`, `Is`, `Iss` and `Via`), up to five name words that are not common function words (a capital `A` directly before the suffix still counts), rural and contract routes, and a unit after a PO box (repo: OfflineCPA/src/Infrastructure/Pii/Detectors/StreetAddressDetector.cs#L1-L94). US phone numbers (repo: OfflineCPA/src/Infrastructure/Pii/Detectors/PhoneNumberDetector.cs#L1-L43) and email addresses (repo: OfflineCPA/src/Infrastructure/Pii/Detectors/EmailDetector.cs#L1-L26) have their own detectors and tokens with no readable part; an email's fingerprint covers its lowercased text, and a street address's covers its lowercased text with spacing and punctuation collapsed (repo: OfflineCPA/src/Domain/Pii/PiiPolicy.cs#L185-L196). Names are still not detected; docs/privacy/masking-coverage.md lists every gap. The ledger use cases mask memos on entry create and reverse (repo: OfflineCPA/src/Application/Ledger/CreateDraftEntry.cs#L30-L35) (repo: OfflineCPA/src/Application/Ledger/ReverseEntry.cs#L30-L33).

## Logs and errors

Every console log line is masked before the provider writes it; every message on the exception chain is checked, and a chain that changes under masking is rebuilt from type names and masked messages (repo: OfflineCPA/src/Backend/Extensions/PiiRedactionLoggerProvider.cs#L14-L142). `Program.cs` clears the default providers and wraps the console provider (repo: OfflineCPA/src/Backend/Program.cs#L24-L29). Upload errors quote input, so the route masks the message before returning it (repo: OfflineCPA/src/Backend/Endpoints/UploadEndpoints.cs#L47-L48).

## Tests and fixtures

Detectors read an ASCII-digit copy of the text, so other scripts' digits mask and match like 0-9 (repo: OfflineCPA/src/Domain/Pii/PiiPolicy.cs#L63-L120). Dot, underscore and slash groupings are detected. A separated account run over 17 digits that would leave any digit readable is masked as one token (repo: OfflineCPA/src/Domain/Pii/PiiPolicy.cs#L122-L183). SSNs and ITINs may mix hyphen and space. Day-first dates and dot-grouped amounts stay unmasked. A log line is withheld when masking fails. The key store creates the file once, owner-only on Unix, and stops startup on a short or unreadable file instead of replacing it.

Coverage, known false positives and negatives, and the contributor warning are in docs/privacy/masking-coverage.md. Synthetic fixture values live in one shared folder so 06 extends them (repo: OfflineCPA/tests/TrueBalance.Tests/Pii/Fixtures/FakePii.cs#L11-L58). The no-leak gate: fake values pass through the masker and a stub importer path, then the test asserts none of them appear in any database text column, captured logs, or an error body (repo: OfflineCPA/tests/TrueBalance.Tests/Pii/PiiNoLeakTests.cs#L23-L217). `ImportNoLeakTests` repeats the check through the real importers (repo: OfflineCPA/tests/TrueBalance.Tests/Imports/ImportNoLeakTests.cs#L14-L19). Statement import (prompt 05) cannot merge until it passes.

## Change it safely

Do not add a detector with a network call or an AI hook; local patterns only. Do not put a real or realistic SSN, EIN, ITIN, routing, or account number in a fixture, log line, or wiki page. Keep [[pii-boundary]] and docs/privacy/masking-coverage.md in sync with every detector change.
