# PII masking coverage and gaps

Status: covers prompt 04b (the masking core, ADR 0010). Prompt 06 extends the detectors, adds encryption at rest, the document vault, and the full no-leak suite.

> **Contributor warning: do not run real bank statements, tax forms, or receipts through any build of TrueBalance before prompt 06 merges.** The masking core below removes account numbers and tax IDs from text the app stores, logs, or returns, but there is no encryption at rest yet, several value types are not detected (see Gaps), and uploaded files are stored unencrypted. Use synthetic fixture data only (see `OfflineCPA/tests/TrueBalance.Tests/Pii/Fixtures/`).

## What the masker does

Text passes through the `IPiiMasker` port (`Application` layer) before it is stored, logged, or echoed in an error. The domain `PiiPolicy` (ADR 0010) replaces every detected value with a non-destructive token:

- The **last 4 digits** stay readable, for display and as the first matching key.
- A **keyed fingerprint** (HMAC-SHA256 over the digits only, lowercase hex) makes two rows holding the same number match without ever storing it. The token embeds the first 16 hex characters; the full digest is available on the `MaskedValue` for callers that store matching keys.
- Separator variants of the same number ("123-45-6789" and "123456789") produce the same fingerprint.
- Amounts, dates, and payee text that is not a detected value are never altered.

The HMAC key is local and stable: set `Pii:FingerprintKey` (base64; generate with `openssl rand -base64 32`) when you want fingerprints tied to an explicit secret. When unset, the app generates a 32-byte key on first start and stores it at `data/pii-fingerprint.key` under the app content root (anchored there, not to the working directory; the pattern `**/pii-fingerprint.key` is git-ignored), so fingerprints survive restarts. Under docker-compose it lives on the `pb-keys` volume at `/app/keys/pii-fingerprint.key`. **Back up the key file: losing it breaks fingerprint matching** for everything masked earlier (prompt 13 covers backups). A key file that is unreadable or too short stops startup and is never overwritten. The key never leaves the machine. Callers that store matching keys use `IPiiMasker.MaskDetailed`, which returns the structured last-4 and full fingerprint per match.

## Detected types

| Type | Pattern | Notes |
| --- | --- | --- |
| SSN | NNN-NN-NNNN (same `-`, space, `.`, `_` or `/` twice) or a bare 9-digit run | Skips area 000, 666, 9xx, group 00, serial 0000. |
| EIN | NN-NNNNNNN (also `.`, `_`, `/`) | The formatted Employer ID shape. |
| ITIN | 9NN-NN-NNNN (4th–5th digits 50–65, 70–88, 90–92, 94–99) and the bare 9xx equivalents | Middle-digit ranges as published by the IRS (IRS Publication 1915: a nine-digit number beginning with 9, fourth and fifth digits from 50–65, 70–88, 90–92, or 94–99 [High 3/3] https://www.irs.gov/pub/irs-pdf/p1915.pdf). |
| ABA routing number | Bare 9-digit run passing the check digit | Weights 3,7,1,3,7,1,3,7,1 with a weighted sum divisible by 10 (Apache Commons Validator ABANumberCheckDigit [High 3/3] https://commons.apache.org/proper/commons-validator/apidocs/org/apache/commons/validator/routines/checkdigit/ABANumberCheckDigit.html). Checksum-valid runs outrank SSN reads. |
| Bank account number | 8–17 digit run, optionally grouped by hyphens or spaces, or by one of `.` `_` `/` used at least twice | The least certain detector; exclusions below. |

Bank-account exclusions: phone-shaped 3-3-4 numbers, YYYYMMDD, YYYY-MM-DD and MM/DD/YYYY dates (same separators), currency amounts (a `$` before the run, or a decimal `.`+digit neighbor), and 9-digit values shaped like an SSN or EIN that failed their own validation (they stay untouched rather than being relabeled). Overlapping matches resolve by type: routing (check digit) first, then SSN, EIN/ITIN, then account number.

## Known false positives (masked even though they are not account numbers)

- A bare nine-digit EIN (no hyphen) is ambiguous: if it is not claimed as a routing number, SSN or ITIN, it is masked as an account number (`[ACCT ****5678 …]`), so it is hidden but carries the wrong type.
- Invoice, order, or reference numbers of 8–17 digits with no date, phone, or amount shape.
- Any other digit run of 8–17 digits, e.g. a long product code.
- IP addresses or version strings with 8–17 digits, and `NN.NNNNNNN` shapes (read as an EIN).
Masking these is non-destructive (last 4 digits and fingerprint stay), but expect tokens where a statement printed such codes.

## Known false negatives (not detected — do not rely on masking for these)

- Account numbers shorter than 8 digits.
- Account numbers with only one `.`, `_` or `/` separator (`1234.56789`), or mixing those separators.
- `.`, `_` or `/` grouped numbers followed by `,` and a digit (read as an amount).
- 8-digit account numbers that read as a YYYYMMDD date (for example `20151231`).
- Digits outside the Basic Multilingual Plane (such as mathematical digit symbols).
- Routing numbers printed with separators (rare) are not detected.
- Card numbers, dates of birth, street addresses, emails, phone numbers as text: prompt 06.
- Values split across columns or misspelled.

## What waits for prompt 06

Encryption at rest, the document vault (uploaded statements stay unencrypted today), card/DOB/address detectors, vendor TIN capture for 1099s, and the full no-leak suite. Prompt 06 extends the detectors and the shared fixture folder instead of forking them.
