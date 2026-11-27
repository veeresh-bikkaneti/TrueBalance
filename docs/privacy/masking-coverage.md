# PII masking coverage and gaps

Status: covers the masking core (prompt 04b, ADR 0010) and the detectors added by prompts 06 and 06c.

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
| SSN | NNN-NN-NNNN (`-` and space may mix; `.`, `_` or `/` must repeat) or a bare 9-digit run | Skips area 000, 666, 9xx, group 00, serial 0000. |
| EIN | NN-NNNNNNN (also `.`, `_`, `/`) | The formatted Employer ID shape. |
| ITIN | 9NN-NN-NNNN (4th–5th digits 50–65, 70–88, 90–92, 94–99) and the bare 9xx equivalents | Middle-digit ranges as published by the IRS (IRS Publication 1915: a nine-digit number beginning with 9, fourth and fifth digits from 50–65, 70–88, 90–92, or 94–99 [High 3/3] https://www.irs.gov/pub/irs-pdf/p1915.pdf). |
| ABA routing number | Bare 9-digit run passing the check digit | Weights 3,7,1,3,7,1,3,7,1 with a weighted sum divisible by 10 (Apache Commons Validator ABANumberCheckDigit [High 3/3] https://commons.apache.org/proper/commons-validator/apidocs/org/apache/commons/validator/routines/checkdigit/ABANumberCheckDigit.html). Checksum-valid runs outrank SSN reads. |
| Bank account number | 8–17 digit run, optionally grouped by hyphens or spaces (double spaces, `--` and ` - ` included), or by one of `.` `_` `/` used at least twice; a run over 17 digits is masked whole. A space-grouped run stays readable only when `Invoice(s)`, `Inv`, `Inv #`, `Check(s)`, `Chk` or `Order(s)` (any case) sits directly before it, every group is 3 to 6 digits, and the run is not a card layout (4-4-4-4, 4-6-5 or 4-6-4) | The least certain detector; exclusions below. An account joined to an SSN by `_` is masked from its first group after the SSN. |
| Card number | 13–19 digits, optionally grouped by single spaces or hyphens, passing the Luhn check | A card-shaped run that fails Luhn falls back to the account detector, which masks the card layouts even after a list label. |
| Date of birth | The date after a label: `DOB`, `D.O.B.`, `date of birth`, `birth date`, `birthdate`, `born`, `born on`, with or without a colon or dash. Numeric dates with a two or four digit year, `YYYY-MM-DD`, and English month names or three-letter forms (`April 12, 1980`, `12 Apr 1980`) | Unlabeled dates are never masked. Keeps no readable digits. |
| Street address | House number, one to five name words (letters, hyphen, apostrophe, `Jr`, `Sr`, ordinals such as `5th`; none of them `a`, `an`, `the`, `to`, `for`, `of`, `at`, `from`, `via`, `in`, `on`, `by`, `with`, `and`, `or`, `this`, `that`, `is`, `was` or `per`, in any case; the one exception is a capital `A` directly before the suffix, as in `100 A Street`), then a street suffix name or abbreviation from USPS Publication 28 Appendix C1 (https://pe.usps.com/text/pub28/28apc_002.htm [Moderate 1/1]) other than `C`, `Un`, `Is`, `Iss` and `Via`; `Rural Route`, `RR`, `HC` and `Highway Contract` routes with their box number (sections 241 and 251: https://pe.usps.com/text/pub28/28c2_021.htm, https://pe.usps.com/text/pub28/28c2_027.htm [Moderate 1/1]); `PO Box` and `P.O. Box` numbers. A following `Apt`, `Suite`, `Ste`, `Unit` or `#` value after a street or PO box is included | Shape check only. Keeps no readable digits. |
| Phone number | US 10-digit numbers split by dashes, dots, spaces or area code brackets, with an optional `+1` or `1` and an optional extension (`ext`, `ext.`, `extension`, `x`, `#`); ten bare digits only after `+1` | Keeps no readable digits. A shape touching more digits may be masked as an account number instead. |
| Email address | Local part, `@`, and a domain with a dot | Keeps no readable part. The fingerprint is over the lowercased address. |

Bank-account exclusions: phone-shaped 3-3-4 numbers (left to the phone detector), YYYYMMDD, YYYY-MM-DD, MM/DD/YYYY, DD/MM/YYYY and DD.MM.YYYY dates (same separators), dot-grouped amounts (`12.345.678`), currency amounts (a `$` before the run, or a decimal `.`+digit neighbor), and 9-digit values shaped like an SSN or EIN that failed their own validation (they stay untouched rather than being relabeled). Overlapping matches resolve by type: email first, then routing (check digit), card, SSN, EIN/ITIN, date of birth and address, phone, then account number. When a phone or address takes only the last group of a longer grouped number, its token widens over the earlier groups.

## Known false positives (masked even though they are not account numbers)

- A bare nine-digit EIN (no hyphen) is ambiguous: if it is not claimed as a routing number, SSN or ITIN, it is masked as an account number (`[ACCT ****5678 …]`), so it is hidden but carries the wrong type.
- Invoice, order, or reference numbers of 8–17 digits with no date, phone, or amount shape.
- Space-separated number lists with no list label directly before them (`Paid 1001 1002 1003`), since they cannot be told from a grouped account number.
- Ordinary phrases shaped like an address (a number, up to five words, then a word such as `Way`, `Park` or `Land`). The suffix list holds common words, so a memo with none of the excluded function words, such as `3 coffee shop park`, is masked. So is a capital `A` directly before such a word (`PAID 50 A COURT FEE`).
- Four 4-digit check or invoice numbers in a row after a list label (`Checks 1001 1002 1003 1004`) are masked, because they look like a card.
- A date or other digits directly before a phone number or address can be swallowed into its token.
- A joined run that happens to pass the Luhn check is masked as one card, even when it holds an account number and an SSN.
- Any other digit run of 8–17 digits, e.g. a long product code.
- IP addresses or version strings with 8–17 digits, and `NN.NNNNNNN` shapes (read as an EIN).
Masking these is non-destructive (last 4 digits and fingerprint stay), but expect tokens where a statement printed such codes.

## Known false negatives (not detected — do not rely on masking for these)

- Account numbers shorter than 8 digits.
- Account numbers with only one `.`, `_` or `/` separator (`1234.56789`), or mixing those separators.
- `.`, `_` or `/` grouped numbers followed by `,` and a digit (read as an amount).
- 8-digit account numbers that read as a YYYYMMDD date (for example `20151231`).
- `1234.5678.9012,5` (read as an amount because of the trailing `,5`).
- `123_456_7890` (read as a phone number).
- `2024-11 25` (mixed separators; its 8 digits read as a YYYYMMDD date).
- Digits outside the Basic Multilingual Plane (such as mathematical digit symbols).
- Routing numbers printed with separators (rare) are not detected.
- People's names: there is no reliable local pattern; prompt 08 protects stored names and tax IDs as fields [Inferred].
- Phone numbers outside the US shapes above: 7-digit local numbers and `_` separators stay readable; ten bare digits and other countries' numbers are at best read as account numbers.
- Email addresses written in words (`jane at example dot com`), without a dot in the domain, with spaces or a quoted local part, or with an IP address as the domain.
- Dates of birth without a label, without a year, split by spaces only (`04 12 1980`), or with non-English month names.
- Addresses with no listed suffix, route or PO box; house numbers with a letter (`12B`) or a fraction (the whole part of `12 1/2` stays readable); `RFD` and `RD` route labels; routes with no box number.
- Addresses with a function word before the suffix, such as `12 Avenue of the Pines`, or a lettered street written with a lowercase `a` (`100 a Street`); island and viaduct addresses written only with `C`, `Un`, `Is`, `Iss` or `Via`.
- Card numbers that fail Luhn are read as account numbers.
- Digit groups split by tabs or non-breaking spaces are not joined into one run.
- An account number written as 3 to 6 digit groups directly after a list label (`Check 1234 5678 9012`) stays readable unless the whole run is a card layout; a card number followed by more groups (`Check 1234 5678 9012 3456 100`) is not that layout.
- Values split across columns or misspelled.

## What waits for later prompts

Vendor TIN capture for 1099s (prompt 08). Later prompts extend the detectors and the shared fixture folder instead of forking them.
