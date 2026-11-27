---
checked:
  OfflineCPA/src/Application/Interfaces/IDocumentVault.cs: da4b7921dd9557cce52304e64a178ab627ebfb74
  OfflineCPA/src/Application/Uploads/StoredUpload.cs: d0f89183d09d583b499dbb4352993efda461b885
  OfflineCPA/src/Application/Interfaces/IStatementImporter.cs: ba4a0fa808dc243bd3d61a74df52ca6b567b3bf0
  OfflineCPA/src/Infrastructure/Options/UploadOptions.cs: 9ae8e83707e682d5fbaa3750ff62686ca5174e68
  OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs: ddabcdea7617797ac1c5251b09e9a1a4496cd84e
  OfflineCPA/src/Backend/Endpoints/UploadEndpoints.cs: 5c7efd00e3d56a2163cfaacab57769403ea3d8e3
  OfflineCPA/src/Infrastructure/Services/PdfFileParser.cs: 60f06e3346140d6e125a62e37376cf13637cfe0e
  OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs: c9c938cf7002f8117b0d97c5db76bec228eda379
  OfflineCPA/tests/TrueBalance.Tests/SecureUploadStoreTests.cs: 466fcc91ef34691cb13c9adc0975d01aa87fca26
  OfflineCPA/tests/TrueBalance.Tests/FolderWatchLogTests.cs: b1d72233acaf1cf3e726003353ad70b68a229d65
---
# Uploads

`IDocumentVault.SaveAsync` (before this rename: `ISecureUploadStore`) takes a stream, a length, the original file name, and a content type, and returns a `StoredUpload` (repo: OfflineCPA/src/Application/Interfaces/IDocumentVault.cs#L9-L15) (repo: OfflineCPA/src/Application/Uploads/StoredUpload.cs#L3-L11). `SecureUploadStore` is the adapter.

`IStatementImporter` is the port for turning one local file into statement rows, with CSV, OFX and PDF adapters (repo: OfflineCPA/src/Application/Interfaces/IStatementImporter.cs#L8-L20). PDF output is experimental. See [[statement-import]].

## What the store checks

Empty files and files over `MaxBytes` (default 20 MB) are rejected. The extension must be in the allow list (`.csv`, `.ofx`, `.qfx`, `.pdf`, `.png`, `.jpg`, `.jpeg`) and the content type, when present, must be in the allow list (repo: OfflineCPA/src/Infrastructure/Options/UploadOptions.cs#L3-L25) (repo: OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs#L29-L50). The stored name is a new GUID in `N` format plus the extension. The full path must stay under the upload root (repo: OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs#L52-L66). `SanitizeFileName` strips the directory part and null bytes. That cleaned name is data on `StoredUpload`, not the path (repo: OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs#L81-L92).

## Entry points

`POST /api/upload` stores the file through the vault and, when an importer supports the extension, queues an import batch. It parses and posts nothing (repo: OfflineCPA/src/Backend/Endpoints/UploadEndpoints.cs#L16-L53).

`LocalFileProcessorService` scans `Bookkeeping:InputDirectory` (default `/app/data/input`) every 30 seconds. For each supported file it first moves the file to `processing`, saves a vault copy, queues a batch, and moves it on to `processed` without overwriting. If queueing fails the file goes back; if the last move fails it stays in `processing`, which is never scanned, so a restart cannot queue it twice. Logs use a generated id, not the file name (repo: OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs#L57-L127).

## Tests

`SecureUploadStoreTests` covers hostile names, the allow list, empty files, and oversize files (repo: OfflineCPA/tests/TrueBalance.Tests/SecureUploadStoreTests.cs#L44-L97). `FolderWatchLogTests` asserts the log has no original file name (repo: OfflineCPA/tests/TrueBalance.Tests/FolderWatchLogTests.cs#L28-L51).

## Change it safely

A new file type goes in a new adapter folder with its own module class; see [[network-gate]] for the convention. Do not build a path from the client file name. Anything that persists or echoes extracted text passes [[pii-masking]] first.
