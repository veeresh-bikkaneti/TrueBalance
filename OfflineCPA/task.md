# Personal Booking - Implementation Tasks

## 1. Domain & Infrastructure (Backend)
- [x] Create [Transaction](file:///d:/TaxReturns/Run_2026%20filing/Business%20Travel-%20india/OfflineCPA/src/Domain/Entities/Transaction.cs#3-18) and [Account](file:///d:/TaxReturns/Run_2026%20filing/Business%20Travel-%20india/OfflineCPA/src/Domain/Entities/Account.cs#5-23) entities in `Domain`.
- [x] Implement a basic Chart of Accounts targeting IRS Form 1120-S.
- [x] Configure PostgreSQL schema for transactions and accounts using Entity Framework Core.

## 2. Services & Endpoints (Backend)
- [ ] Implement `IHostedService` or `FileSystemWatcher` to monitor a local `.env` specified directory.
- [ ] Implement 100% offline text extraction (receipts/PDFs) mapping to transactions.
- [ ] Implement an Excel export endpoint using `ClosedXML` to generate `.xlsx` file securely in memory.

## 3. Web Client (Frontend)
- [ ] Create the Tax Preparation Dashboard (1120-S) view.
- [ ] Establish connecting UI to view categorized transactions.
- [ ] Implement Local Directory Configuration settings (to tell the backend where to monitor).
- [ ] Implement the "Export Book to Excel" button connected to the backend download.

## 4. Final Security QA
- [ ] Execute validation checks to ensure zero-cloud leakage.
- [ ] Verify Docker container mounts correctly bridge the frontend and backend strictly locally.
