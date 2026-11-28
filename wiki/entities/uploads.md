---
checked:
  OfflineCPA/src/Application/Interfaces/IDocumentVault.cs: 8d3b7ddb7f73004bd41d40945ee4c05cac3b97ea
  OfflineCPA/src/Application/Uploads/StoredUpload.cs: d0f89183d09d583b499dbb4352993efda461b885
  OfflineCPA/src/Infrastructure/Vault/VaultModule.cs: 7aa8b0627d4d668f174506877f24f62d8bbdc862
  OfflineCPA/src/Application/Interfaces/IStatementImporter.cs: 815216298b5b0524f1ab804254586bce3abc1586
  OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs: 9a4bc32f73a924aadef0424b4a421cf2f514689d
  OfflineCPA/src/Backend/Endpoints/VaultEndpoints.cs: aeaf4d8666f244a80afa7b55dc081f0c2cbac45c
  OfflineCPA/src/Application/Uploads/PurgeOriginals.cs: 0be9488eb3fbae8596b74925e8332eb09fe16df1
  OfflineCPA/src/Infrastructure/Vault/WrongTryThrottle.cs: 1568d5f1c120ee3f7ae8b182e34ed70440247f0d
  OfflineCPA/src/Application/Uploads/IWrongTryThrottle.cs: 70717e8675ab8452d4777b8110809005989b768f
  OfflineCPA/src/Infrastructure/Vault/ThrottleModule.cs: a490e8482a453e85a283c3ba7a137e4711cad342
  OfflineCPA/frontend/src/features/vault/route.tsx: d19f53490e680f7a04efac3188b2dd035912be46
  OfflineCPA/frontend/src/features/vault/VaultScreen.tsx: 3966ec3281dd6c723c2584c8f2c6866ee34d34ba
  OfflineCPA/frontend/src/features/vault/SecretForm.tsx: 98a730526d8d4ce6af64e0cd010d2dfe403bdf1a
  OfflineCPA/frontend/src/features/vault/recovery/CodeOnce.tsx: b1219702a5859b4cbbbc96f639ca17810c7768af
  OfflineCPA/src/Backend/Endpoints/UploadEndpoints.cs: 8eebefad33f134ade84e8f918dda3c647dbe3be3
  OfflineCPA/src/Backend/Program.cs: 4000a270a06e4648a68e0d0b9f83186ef7776106
  OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs: 386a38d5ea989a34871a17c5bc3c26a2d4626304
  OfflineCPA/tests/TrueBalance.Tests/Vault/VaultTests.cs: 6047014d8e6f8dfd43cb0c562f10f28293a4df6e
  OfflineCPA/tests/TrueBalance.Tests/Vault/VaultPassphraseTests.cs: 875c63ee01af902e359113a674b961bad063a43a
  OfflineCPA/tests/TrueBalance.Tests/Vault/VaultRecoveryTests.cs: 4cc1089dcf4830fe8fdff0c0b31a3d67898c4c13
  OfflineCPA/tests/TrueBalance.Tests/Vault/WrongTryThrottleTests.cs: f39c583c5fcb80ec0f9bd1288b2bb281ecbaa897
  OfflineCPA/frontend/src/features/vault/vault.test.tsx: b348f24c44658f950a52122464e8fc882c5695d1
  OfflineCPA/tests/TrueBalance.Tests/SecureUploadStoreTests.cs: 1466e7c579a91cd7d48c19b61ff6db726aed373b
  OfflineCPA/tests/TrueBalance.Tests/FolderWatchLogTests.cs: 233e7d97e6762cc53b49014f64494eb1031c6230
---
# Uploads

`IDocumentVault.SaveAsync` takes a stream, a length, the original file name, and a content type, and returns a `StoredUpload` (repo: OfflineCPA/src/Application/Interfaces/IDocumentVault.cs#L9-L28) (repo: OfflineCPA/src/Application/Uploads/StoredUpload.cs#L3-L9). `EncryptedFileVault` is the adapter, registered once so it can hold the key (repo: OfflineCPA/src/Infrastructure/Vault/VaultModule.cs#L9-L15).

`IStatementImporter` is the port for turning one file's content into statement rows, with CSV, OFX and PDF adapters (repo: OfflineCPA/src/Application/Interfaces/IStatementImporter.cs#L10-L15). It reads a stream, never a path. PDF output is experimental. See [[statement-import]].

## What the vault checks

Empty files and files over `MaxBytes` (default 20 MB) are rejected, by declared length and by bytes read. The extension must be in the allow list and the content type, when present, must be too (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L157-L207). The stored name is a new GUID in `N` format plus the extension, with `.enc` on disk. `SanitizeFileName` strips the directory part and null bytes; that cleaned name is data on `StoredUpload`, not the path (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L233-L244).

## Encryption and unlock

Files are sealed with AES-256-GCM: nonce, tag, ciphertext, with the file name authenticated (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L422-L431). A random 32-byte data key seals the files, and stays in memory while the vault is unlocked. `vault.json` (version 2) holds that key sealed with AES-256-GCM by a second key, PBKDF2-HMAC-SHA256 over the passphrase with its own salt (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L419-L420) (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L348-L362). The `Recovery` slot is null until a code is made. The first unlock creates `vault.json`; missing settings beside sealed files, damaged settings and a wrong passphrase all fail closed (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L267-L330). Unlock also seals leftover plain uploads and deletes each only after it opens again (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L389-L408).

## Changing the passphrase

A change opens the data key with the current passphrase and seals it again under the new one, which needs at least 12 characters. No stored document is touched (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L73-L92). On the first unlock of a version 1 vault, its passphrase key becomes the data key, so no document is encrypted again (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L284-L293). `vault.json` is written to a temporary file, flushed, then renamed over the old one (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L365-L375).

A change does not replace the data key. Someone who has an older copy of `vault.json` and the passphrase it was made with can still recover that key. For a vault upgraded from version 1, the old passphrase and the old salt are enough. (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L284-L293) (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L348-L362)

## Recovery code

An unlocked vault, given its passphrase again, can make a recovery code: 128 random bits shown as eight groups of four. The `Recovery` slot then holds the data key sealed by a key derived from the code, with its own salt (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L94-L117). The request needs the JSON body `{"passphrase": "..."}`; a wrong one changes nothing. The code is returned once and is not stored. A new code replaces the slot, so the old code stops working. Recovery opens the data key with the code, sets a new passphrase and unlocks the vault; a wrong code gives the same message as a wrong passphrase (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L119-L152). A code works once: recovery clears the slot in the same settings write, and the response says to make a new one. A passphrase change keeps the code. Owner instructions are in `docs/privacy/vault-recovery.md`.

Routes: `POST /api/vault/unlock`, `POST /api/vault/change-passphrase`, `POST /api/vault/recovery-code`, `POST /api/vault/recover` and `POST /api/vault/purge-originals`, plus `GET /api/vault/status`, which says whether the vault is unlocked and whether `vault.json` exists (repo: OfflineCPA/src/Backend/Endpoints/VaultEndpoints.cs#L23-L73). Purge needs a confirmation and an unlocked vault, returns an IRS retention warning, and deletes stored documents only (repo: OfflineCPA/src/Application/Uploads/PurgeOriginals.cs#L8-L19).

## Wrong-try slowdown

Unlock, change-passphrase, recovery-code and recover share one count of wrong answers, kept in memory, so a restart forgets it. After 3 in a row, the next try waits 1 second, then double each time, at most 5 minutes; a right answer resets the count (repo: OfflineCPA/src/Infrastructure/Vault/WrongTryThrottle.cs#L9-L94). While waiting, the route answers 429 with `Retry-After` and one plain message, and the vault is never asked. Only the vault's wrong-secret message counts: a short new passphrase or a locked vault neither counts nor resets (repo: OfflineCPA/src/Backend/Endpoints/VaultEndpoints.cs#L75-L102). One secret is checked at a time, so guesses sent together cannot slip past the count; the extra ones get 429 too (repo: OfflineCPA/src/Application/Uploads/IWrongTryThrottle.cs#L3-L17). `ThrottleModule` registers it (repo: OfflineCPA/src/Infrastructure/Vault/ThrottleModule.cs#L6-L13).

## Vault screen

`#/vault` shows locked or unlocked, sets the first passphrase (typed twice, at least 12 characters), unlocks, changes the passphrase, makes a recovery code and recovers with one (repo: OfflineCPA/frontend/src/features/vault/route.tsx#L1-L4) (repo: OfflineCPA/frontend/src/features/vault/VaultScreen.tsx#L16-L104). Passphrases and typed codes live in form state and are cleared as each call is sent (repo: OfflineCPA/frontend/src/features/vault/SecretForm.tsx#L46-L65). A new code is shown until the "I wrote it down" tick, and is stored nowhere (repo: OfflineCPA/frontend/src/features/vault/recovery/CodeOnce.tsx#L1-L14).

## Entry points

`POST /api/upload` stores the file through the vault and, when an importer supports the extension, queues an import batch (repo: OfflineCPA/src/Backend/Endpoints/UploadEndpoints.cs#L16-L52). Form bodies stay in memory so no plain temporary file is written (repo: OfflineCPA/src/Backend/Program.cs#L73-L78).

`LocalFileProcessorService` scans `Bookkeeping:InputDirectory` every 30 seconds. While the vault is locked it leaves files where they are. Otherwise it moves a file to `processing`, saves a vault copy, queues a batch, and deletes the staging file. If queueing fails the file goes back (repo: OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs#L57-L117).

## Tests

`VaultTests` covers the round trip, wrong passphrase, tampering, locked refusals, plain-text absence, folder intake, older uploads and purge (repo: OfflineCPA/tests/TrueBalance.Tests/Vault/VaultTests.cs#L21-L65). `VaultPassphraseTests` covers the version 1 upgrade, the passphrase change, the interrupted write and passphrase-free logs (repo: OfflineCPA/tests/TrueBalance.Tests/Vault/VaultPassphraseTests.cs#L81-L226). `VaultRecoveryTests` covers the recovery code (repo: OfflineCPA/tests/TrueBalance.Tests/Vault/VaultRecoveryTests.cs#L38-L226). `WrongTryThrottleTests` covers the slowdown and the status route (repo: OfflineCPA/tests/TrueBalance.Tests/Vault/WrongTryThrottleTests.cs#L116-L283), and `vault.test.tsx` the screen (repo: OfflineCPA/frontend/src/features/vault/vault.test.tsx#L68-L160). `SecureUploadStoreTests` keeps the hostile-name and allow-list cases against the vault (repo: OfflineCPA/tests/TrueBalance.Tests/SecureUploadStoreTests.cs#L1-L40). `FolderWatchLogTests` asserts the log has no original file name (repo: OfflineCPA/tests/TrueBalance.Tests/FolderWatchLogTests.cs#L28-L51).

## Change it safely

A new file type goes in a new adapter folder with its own module class; see [[network-gate]] for the convention. Do not build a path from the client file name, and do not write plain content to disk. Anything that persists or echoes extracted text passes [[pii-masking]] first.
