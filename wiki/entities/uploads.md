---
checked:
  OfflineCPA/src/Application/Interfaces/IDocumentVault.cs: 8d3b7ddb7f73004bd41d40945ee4c05cac3b97ea
  OfflineCPA/src/Application/Uploads/StoredUpload.cs: d0f89183d09d583b499dbb4352993efda461b885
  OfflineCPA/src/Application/Interfaces/IStatementImporter.cs: 815216298b5b0524f1ab804254586bce3abc1586
  OfflineCPA/src/Infrastructure/Options/UploadOptions.cs: 9ae8e83707e682d5fbaa3750ff62686ca5174e68
  OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs: 7ece069c6c1c068bcf26fe6e7932bbd8c2551059
  OfflineCPA/src/Infrastructure/Vault/VaultModule.cs: 7aa8b0627d4d668f174506877f24f62d8bbdc862
  OfflineCPA/src/Backend/Endpoints/VaultEndpoints.cs: fb4cc6cf911dec518d89439b1feefe1774366600
  OfflineCPA/src/Backend/Endpoints/UploadEndpoints.cs: 8eebefad33f134ade84e8f918dda3c647dbe3be3
  OfflineCPA/src/Application/Uploads/PurgeOriginals.cs: 0be9488eb3fbae8596b74925e8332eb09fe16df1
  OfflineCPA/src/Backend/Program.cs: 4000a270a06e4648a68e0d0b9f83186ef7776106
  OfflineCPA/src/Infrastructure/Services/PdfFileParser.cs: b269b192cbf46fbd51d256ac9404d0be8f8b31df
  OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs: 386a38d5ea989a34871a17c5bc3c26a2d4626304
  OfflineCPA/tests/TrueBalance.Tests/SecureUploadStoreTests.cs: 1466e7c579a91cd7d48c19b61ff6db726aed373b
  OfflineCPA/tests/TrueBalance.Tests/FolderWatchLogTests.cs: 233e7d97e6762cc53b49014f64494eb1031c6230
  OfflineCPA/tests/TrueBalance.Tests/Vault/VaultTests.cs: 6047014d8e6f8dfd43cb0c562f10f28293a4df6e
---
# Uploads

`IDocumentVault.SaveAsync` takes a stream, a length, the original file name, and a content type, and returns a `StoredUpload` (repo: OfflineCPA/src/Application/Interfaces/IDocumentVault.cs#L9-L28) (repo: OfflineCPA/src/Application/Uploads/StoredUpload.cs#L3-L9). `EncryptedFileVault` is the adapter, registered once so it can hold the key (repo: OfflineCPA/src/Infrastructure/Vault/VaultModule.cs#L9-L15).

`IStatementImporter` is the port for turning one file's content into statement rows, with CSV, OFX and PDF adapters (repo: OfflineCPA/src/Application/Interfaces/IStatementImporter.cs#L10-L15). It reads a stream, never a path. PDF output is experimental. See [[statement-import]].

## What the vault checks

Empty files and files over `MaxBytes` (default 20 MB) are rejected, by declared length and by bytes read. The extension must be in the allow list and the content type, when present, must be too (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L61-L110). The stored name is a new GUID in `N` format plus the extension, with `.enc` on disk. `SanitizeFileName` strips the directory part and null bytes; that cleaned name is data on `StoredUpload`, not the path (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L137-L147).

## Encryption and unlock

Files are sealed with AES-256-GCM: nonce, tag, ciphertext, with the file name authenticated (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L253-L259). The key is PBKDF2-HMAC-SHA256 over the passphrase (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L248-L249) and stays in memory. The first unlock creates `vault.json`; missing settings beside sealed files, damaged settings and a wrong passphrase all fail closed (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L163-L180). Unlock also seals leftover plain uploads and deletes each only after it opens again (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L218-L236).

Routes: `POST /api/vault/unlock` and `POST /api/vault/purge-originals` (repo: OfflineCPA/src/Backend/Endpoints/VaultEndpoints.cs#L12-L32). Purge needs a confirmation and an unlocked vault, returns an IRS retention warning, and deletes stored documents only (repo: OfflineCPA/src/Application/Uploads/PurgeOriginals.cs#L8-L19).

## Entry points

`POST /api/upload` stores the file through the vault and, when an importer supports the extension, queues an import batch (repo: OfflineCPA/src/Backend/Endpoints/UploadEndpoints.cs#L16-L52). Form bodies stay in memory so no plain temporary file is written (repo: OfflineCPA/src/Backend/Program.cs#L73-L78).

`LocalFileProcessorService` scans `Bookkeeping:InputDirectory` every 30 seconds. While the vault is locked it leaves files where they are. Otherwise it moves a file to `processing`, saves a vault copy, queues a batch, and deletes the staging file. If queueing fails the file goes back (repo: OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs#L57-L117).

## Tests

`VaultTests` covers the round trip, wrong passphrase, tampering, locked refusals, plain-text absence, folder intake, older uploads and purge (repo: OfflineCPA/tests/TrueBalance.Tests/Vault/VaultTests.cs#L21-L65). `SecureUploadStoreTests` keeps the hostile-name and allow-list cases against the vault (repo: OfflineCPA/tests/TrueBalance.Tests/SecureUploadStoreTests.cs#L1-L40). `FolderWatchLogTests` asserts the log has no original file name (repo: OfflineCPA/tests/TrueBalance.Tests/FolderWatchLogTests.cs#L28-L51).

## Change it safely

A new file type goes in a new adapter folder with its own module class; see [[network-gate]] for the convention. Do not build a path from the client file name, and do not write plain content to disk. Anything that persists or echoes extracted text passes [[pii-masking]] first.
