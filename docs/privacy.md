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

## Uploads are encrypted

Every new upload and every folder-watch file is sealed with AES-256-GCM before it touches the disk. Each file gets a fresh nonce, and the file name is part of the seal, so one file cannot stand in for another.

- **The key** comes from your passphrase (at least 12 characters) through PBKDF2 with HMAC-SHA-256, 600,000 iterations and a random 16-byte salt. The salt, the settings and a key check value sit in `vault.json` beside the files. The key itself is held in memory only, so a restart locks the books again.
- **Unlock** with `POST /api/vault/unlock`. The first unlock creates the vault. While it is locked, upload, folder intake, import and preview answer "Unlock the books first."
- **Older plain uploads** are encrypted at the next unlock. Each one is opened and compared before the plain copy is deleted.
- **Folder intake** deletes the file you dropped in once the vault holds it. Files that an older version left in the input folder's `processed` subfolder are still plain: delete them yourself.
- **Purge originals** with `POST /api/vault/purge-originals` and `{"confirm": true}`. It deletes stored documents only, never ledger entries or staged rows.

**There is no recovery.** If you forget the passphrase, or lose `vault.json`, the stored documents cannot be opened. Back both up.

Why PBKDF2 and not Argon2id: OWASP puts Argon2id first (at least 19 MiB, 2 iterations, 1 lane) and PBKDF2-HMAC-SHA-256 at 600,000 iterations where FIPS-140 matters [Moderate 1/1: https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html]. PBKDF2 is built into .NET and works offline with no new package; Argon2id resists guessing on graphics cards better and would need one [Inferred]. The owner chose PBKDF2. NIST asks for a salt of at least 128 bits [Moderate 1/1: NIST SP 800-132, https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-132.pdf].

What encryption does not cover: the database (it holds masked text), a computer that is unlocked and already compromised, memory or swap while the app runs, and copies your backups made earlier.

## How long to keep records

The IRS often says 3 years; some cases call for 6 or 7 years, or indefinitely, and employment tax records at least 4 years [Moderate 1/1: https://www.irs.gov/businesses/small-businesses-self-employed/how-long-should-i-keep-records]. Check your case with a CPA before you purge.

## Keep less, keep it shorter

The NIST guide to protecting personally identifiable information recommends limiting the use, collection and retention of personal data to what a business purpose strictly needs, and reviewing what you already hold [Moderate 1/1: NIST SP 800-122, https://nvlpubs.nist.gov/nistpubs/legacy/sp/nistspecialpublication800-122.pdf; wording taken from search-result excerpts, not the full PDF]. That is the direction we follow: mask early, keep only the tokens the books need, and purge original files when you no longer need them.

## Known gaps

- Nothing purges files on a schedule. You decide when.
- There is no passphrase change yet, and no unlock screen (prompt 10).
- Masking is pattern based. It can miss values written in an unusual shape, and it can mask a number that is not personal. See [masking-coverage.md](privacy/masking-coverage.md).

This is not legal or tax advice. TrueBalance keeps records a CPA can review. Ask a CPA or an attorney about your own retention duties.
