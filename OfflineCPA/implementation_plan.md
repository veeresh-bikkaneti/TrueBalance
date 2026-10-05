# Multi-Agent Orchestration Plan: OfflineCPA Audit

## Goal Description
The user wants a comprehensive, multi-agent orchestrated review of the `OfflineCPA` application to ensure it:
1. Meets IRS standards for S-Corp/Schedule C tax filing (Form 1120-S).
2. Has all key features expected from a modern bookkeeping solution.
3. Allows users to export financial data to a spreadsheet.
4. Allows users to easily provide a local filesystem folder to host `.pdf`, `img`, and `.csv` files for local processing and updating bookkeeping logs.

This review requires collaboration across multiple domains: Product Ownership, Architecture, UI/UX Design, Security, and Code Implementation.

***

## Orchestration Workflow

### Phase 1: Product & Architecture Review
**Agents Involved**: `product-owner`, `project-planner`, `architecture`

**Objectives**:
- Validate the application features against standard bookkeeping requirements and IRS Form 1120-S needs.
- Review the architecture for local file processing (PDFs, images) without external data leakage.
- Ensure the export-to-spreadsheet feature design is robust and accurate.

### Phase 2: Design & User Experience Review
**Agents Involved**: `frontend-specialist`, `web-design-guidelines`

**Objectives**:
- Review the Vite/React frontend UI/UX for the "local folder drop" feature to ensure it's intuitive and accessible.
- Audit the data grids/tables for reviewing bookkeeping logs.
- Provide a responsive and clean method to trigger the "Export to Spreadsheet" action.

### Phase 3: Security & Data Privacy Review
**Agents Involved**: `security-auditor`, `penetration-tester`

**Objectives**:
- Verify that the local processing of PDFs/Images remains strictly local (100% offline processing).
- Perform a security audit on the file upload/folder monitoring mechanisms to prevent path traversal or malicious file execution.
- Ensure PII/PHI sanitization rules remain enforced during export and rendering.

### Phase 4: Implementation Verification
**Agents Involved**: `backend-specialist`, `database-architect`, `test-engineer`

**Objectives**:
- Review the C# (.NET 8) backend logic for parsing CSVs and processing PDFs locally.
- Review the PostgreSQL database schema to ensure it captures all necessary data points for IRS standards.
- Ensure proper test coverage exists for the export capabilities.

***

## Expected Deliverables
1. **Product Gap Analysis**: What IRS features are missing?
2. **Security Sign-off**: Confirmation of 100% local, secure processing.
3. **Architecture Recommendations**: How to best implement the local folder monitoring and spreadsheet export.
4. **UI Mockups/Guidelines**: How the interface should handle local file ingestion.

***

## Orchestrator Findings & Gap Analysis
As the Orchestrator, I simulated the roles of the specialized agents to review the current state of `OfflineCPA`. Here are the findings:

### 1. Product Owner Review (IRS 1120-S & Bookkeeping)
- **Current State**: The application scaffolds a basic "WeatherForecast" API but has **zero** domain logic for bookkeeping.
- **Missing IRS 1120-S Features**:
  - Chart of Accounts specific to S-Corp requirements (Ordinary Business Income, Salaries/Wages, Rents, Taxes, Depreciation, Officer Compensation).
  - Categorization engine to map transactions directly to 1120-S line items.
  - Balance Sheet generation (Schedule L) capabilities.
- **Missing Bookkeeping Features**:
  - Transaction ledger (Income vs. Expense).
  - Double-entry logging.

### 2. Architecture & Solution Design Review
- **Current State**: No endpoints exist for file processing or data export.
- **Gap Analysis**:
  - **Local File Processing**: Needs a C# background service (`IHostedService` or `FileSystemWatcher`) to monitor a local `.env`-configured directory for new PDFs and images.
  - **Spreadsheet Export**: Needs an endpoint in the C# backend utilizing a library like `ClosedXML` or `EPPlus` to generate `.xlsx` files securely entirely in-memory and return as a FileStreamResult.

### 3. Frontend / UI/UX Design Review
- **Current State**: Default Vite/React "Counter" scaffolding.
- **Gap Analysis**:
  - Needs a dedicated "Tax Preparation (1120-S)" dashboard view.
  - Needs a configuration UI to select the local filesystem directory path.
  - Needs a prominent "Export Book to Excel" button triggering the secure download.

### 4. Security & Pen-Tester Review
- **Current State**: Evaluated clean, isolated containers.
- **Sign-off Requirements**:
  - The local folder processor must strictly validate file types (only PDF, PNG, JPEG, CSV) and prevent executable execution.
  - The application must NEVER include third-party OCR APIs (like Google Vision or AWS Textract). All text extraction from PDFs/Images must use 100% local open-source libraries (e.g., `Tesseract.NET` or `iText7` without cloud bindings).
