---
checked:
  OfflineCPA/src/Application/Interfaces/IDocumentVault.cs: e81b2e70ec0d6f40e6a3685caa28418dcad78053
  OfflineCPA/src/Application/Uploads/StoredUpload.cs: d0f89183d09d583b499dbb4352993efda461b885
  OfflineCPA/src/Application/Interfaces/IStatementImporter.cs: 26c011a1a3334924f6f8f2974c188b6ae09456a1
  OfflineCPA/src/Infrastructure/Options/UploadOptions.cs: 9ae8e83707e682d5fbaa3750ff62686ca5174e68
  OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs: 10ac66ed94a52da92f59d8a9355792bf36828413
  OfflineCPA/src/Backend/Endpoints/UploadEndpoints.cs: 404f93eb7df6a91fd45c0087b80953bf0b8c3e9f
  OfflineCPA/src/Infrastructure/Services/PdfFileParser.cs: 761889026e5904f87832e4dd745add680d3f5dcc
  OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs: 274d180fd385ea73989c33d0f8f0312620adf476
  OfflineCPA/tests/TrueBalance.Tests/SecureUploadStoreTests.cs: 466fcc91ef34691cb13c9adc0975d01aa87fca26
  OfflineCPA/tests/TrueBalance.Tests/FolderWatchLogTests.cs: 65987fb121d282db11bdbf832c1c81d48a68c7f8
---
# Uploads

`IDocumentVault.SaveAsync` (before this rename: `ISecureUploadStore`) takes a stream, a length, the original file name, and a content type, and returns a `StoredUpload` (repo: OfflineCPA/src/Application/Interfaces/IDocumentVault.cs#L9-L15) (repo: OfflineCPA/src/Application/Uploads/StoredUpload.cs#L3-L11). `SecureUploadStore` is the adapter.

`IStatementImporter` (before this rename: `ILocalFileParser`) is the port for turning one local file into text; `PdfFileParser` is the PDF adapter (repo: OfflineCPA/src/Application/Interfaces/IStatementImporter.cs#L6-L10).

## What the store checks

Empty files and files over `MaxBytes` (default 20 MB) are rejected. The extension must be in the allow list (`.csv`, `.ofx`, `.qfx`, `.pdf`, `.png`, `.jpg`, `.jpeg`) and the content type, when present, must be in the allow list (repo: OfflineCPA/src/Infrastructure/Options/UploadOptions.cs#L3-L25) (repo: OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs#L29-L50). The stored name is a new GUID in `N` format plus the extension. The full path must stay under the upload root (repo: OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs#L52-L66). `SanitizeFileName` strips the directory part and null bytes. That cleaned name is data on `StoredUpload`, not the path (repo: OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs#L69-L80).

## Entry points

`POST /api/upload` lives in `UploadEndpoints` and calls the vault, then, for a PDF, extracts text through `IStatementImporter` and inserts a `Transaction` whose description carries the vault id plus the masked display name; `ReferenceDocumentPath` stores that id, never a path (repo: OfflineCPA/src/Backend/Endpoints/UploadEndpoints.cs#L40-L70). `PdfFileParser` supports only `.pdf` and reads text with PdfPig (repo: OfflineCPA/src/Infrastructure/Services/PdfFileParser.cs#L6-L26).

`LocalFileProcessorService` scans `Bookkeeping:InputDirectory` (default `/app/data/input`) every 30 seconds. The log line uses a generated id, not the file name. It resolves `IStatementImporter` from a scope (repo: OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs#L55-L76). The transaction stores the generated id in both the description and `ReferenceDocumentPath`; the file name and path never reach the database (repo: OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs#L70-L81).

## Tests

`SecureUploadStoreTests` covers hostile names, the allow list, empty files, and oversize files (repo: OfflineCPA/tests/TrueBalance.Tests/SecureUploadStoreTests.cs#L44-L97). `FolderWatchLogTests` asserts the log has no original file name (repo: OfflineCPA/tests/TrueBalance.Tests/FolderWatchLogTests.cs#L28-L51).

## Change it safely

A new file type goes in a new adapter folder with its own module class; see [[network-gate]] for the convention. Do not build a path from the client file name. Anything that persists or echoes extracted text passes [[pii-masking]] first.
