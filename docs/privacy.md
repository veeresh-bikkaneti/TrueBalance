# Privacy

TrueBalance keeps your books on your machine. Bank statements, W-2s and W-9s are personal, so the app is built to hold as little raw personal data as it can, and it never sends that data to an AI model.

This page describes what the code does today. Coverage gaps are listed in [masking-coverage.md](privacy/masking-coverage.md).

## What gets masked

Before a value is stored, logged or echoed in an error, the masking core replaces it with a token:

- Social Security numbers, EIN and ITIN numbers, bank account numbers, and ABA routing numbers (the original masking core).
- Card numbers that pass a Luhn check.
- Dates of birth, but only when a label says so ("DOB", "date of birth", "born on"). Other dates stay readable so statements still make sense.
- US-style street addresses.

A token looks like `[SSN ****6789 fp:…]`. The last four digits stay readable so you can recognize a value. Dates of birth and street addresses keep no readable digits, only the fingerprint. The fingerprint is a keyed hash, so two rows holding the same number still match without the number being stored.

Free-text journal memos pass through the same masking before they are saved.

## What never leaves the machine

- Every detector is a local pattern check. There are no network calls, no lookup services, and no AI SDKs in the masking code.
- No AI model receives raw personal data. The `IAiAssist` port does not have an implementation yet. When one exists, it will receive tokenized text only, it will ship disabled, and every use will appear in the network log.

## Keep less, keep it shorter

The NIST guide to protecting personally identifiable information recommends limiting the use, collection and retention of personal data to what a business purpose strictly needs, and reviewing what you already hold [Moderate 1/1: NIST SP 800-122, https://nvlpubs.nist.gov/nistpubs/legacy/sp/nistspecialpublication800-122.pdf; wording taken from search-result excerpts, not the full PDF]. That is the direction we follow: mask early, keep only the tokens the books need, and plan to purge original files once they are imported.

## Known gaps

- Uploads are not encrypted yet. The encrypted vault is planned for the follow-up prompt 06b. Until it lands, do not load real documents into a build you do not control.
- Purging original files and the IRS retention guidance are not built yet.
- Masking is pattern based. It can miss values written in an unusual shape, and it can mask a number that is not personal. See [masking-coverage.md](privacy/masking-coverage.md).

This is not legal or tax advice. TrueBalance keeps records a CPA can review. Ask a CPA or an attorney about your own retention duties.
