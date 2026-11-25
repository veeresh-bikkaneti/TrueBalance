---
checked:
  OfflineCPA/src/Application/Interfaces/ISecureUploadStore.cs: 94f8a504c149241710c7a2be2016196b5999dbbe
  OfflineCPA/src/Application/Uploads/StoredUpload.cs: d0f89183d09d583b499dbb4352993efda461b885
  OfflineCPA/src/Infrastructure/Options/UploadOptions.cs: 9ae8e83707e682d5fbaa3750ff62686ca5174e68
  OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs: b7407284bbe0b6fccb23e4623d767c8e499e21f4
  OfflineCPA/src/Backend/Program.cs: 631e0233ec079f91233b2c85b031567189a4e932
  OfflineCPA/src/Infrastructure/Services/PdfFileParser.cs: 8f7a35a74733f38ac324f70b9b2045bf156f70ea
  OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs: 26fef6f6cb06d22e29bd2fe2d51958a635cf2979
  OfflineCPA/tests/TrueBalance.Tests/SecureUploadStoreTests.cs: 466fcc91ef34691cb13c9adc0975d01aa87fca26
  OfflineCPA/tests/TrueBalance.Tests/FolderWatchLogTests.cs: f4efa23bddb3a0e4f82993a6c3a1d2083d437be4
---
# Uploads

`ISecureUploadStore.SaveAsync` takes a stream, a length, the original file name, and a content type, and returns a `StoredUpload` (repo: OfflineCPA/src/Application/Interfaces/ISecureUploadStore.cs#L5-L13) (repo: OfflineCPA/src/Application/Uploads/StoredUpload.cs#L3-L11). `SecureUploadStore` is the adapter.

## What the store checks

Empty files and files over `MaxBytes` (default 20 MB) are rejected. The extension must be in the allow list (`.csv`, `.ofx`, `.qfx`, `.pdf`, `.png`, `.jpg`, `.jpeg`) and the content type, when present, must be in the allow list (repo: OfflineCPA/src/Infrastructure/Options/UploadOptions.cs#L3-L25) (repo: OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs#L29-L50). The stored name is a new GUID in `N` format plus the extension. The full path must stay under the upload root (repo: OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs#L52-L66). `SanitizeFileName` strips the directory part and null bytes. That cleaned name is data on `StoredUpload`, not the path (repo: OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs#L69-L80).

## Entry points

`POST /api/upload` calls the store, then, for a PDF, extracts text and inserts a `Transaction` whose description includes the original file name (repo: OfflineCPA/src/Backend/Program.cs#L73-L122). `PdfFileParser` supports only `.pdf` and reads text with PdfPig (repo: OfflineCPA/src/Infrastructure/Services/PdfFileParser.cs#L6-L26).

`LocalFileProcessorService` scans `Bookkeeping:InputDirectory` (default `/app/data/input`) every 30 seconds. The log line uses a generated id, not the file name. The transaction description still includes `Path.GetFileName` (repo: OfflineCPA/src/Backend/Services/LocalFileProcessorService.cs#L48-L78).

## Tests

`SecureUploadStoreTests` covers hostile names, the allow list, empty files, and oversize files (repo: OfflineCPA/tests/TrueBalance.Tests/SecureUploadStoreTests.cs#L44-L97). `FolderWatchLogTests` asserts the log has no original file name (repo: OfflineCPA/tests/TrueBalance.Tests/FolderWatchLogTests.cs#L28-L51).

## Change it safely

A new allow-list entry belongs in `UploadOptions` and in a store test. Do not build a path from the client file name. Masking of the name stored on the transaction is planned in [[pii-boundary]], not in this store.
