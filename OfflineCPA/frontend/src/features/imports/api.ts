import { api } from '../../shared/api';

// The server sends these three as numbers (Domain/Enums/ImportStatuses.cs).
export const BatchStatus = { Queued: 0, Staged: 1, Failed: 2 } as const;
export const RowStatus = { Pending: 0, Duplicate: 1, Posted: 2, Discarded: 3 } as const;
/** 0 follows the account type, 1 is money available, 2 is an amount owed. */
export type BalanceMeaning = 0 | 1 | 2;

export type AmountLayout = 'Signed' | 'SignedReversed' | 'DebitCredit' | 'Indicator';

export interface Batch {
  id: string;
  extension: string;
  status: number;
  failureReason: string | null;
  balanceReason: string | null;
  createdAtUtc: string;
}

export interface Account {
  accountId: string;
  code: string;
  name: string;
}

export interface CsvColumns {
  date: string;
  description: string;
  amount: string | null;
  debit: string | null;
  credit: string | null;
  balance: string | null;
  dateFormat: string | null;
  amountLayout: AmountLayout;
  debitsCarryMinus: boolean;
  indicatorColumn: string | null;
  moneyInMarker: string | null;
  moneyOutMarker: string | null;
}

export interface Layout {
  amountLayout: AmountLayout;
  debitsCarryMinus: boolean;
  indicatorColumn: string | null;
  moneyInMarker: string | null;
  moneyOutMarker: string | null;
  balanceMeaning: BalanceMeaning | null;
}

export interface Preview {
  layout: Layout;
  balanceCheck: 'NotAvailable' | 'Passed' | 'Failed';
  rows: { date: string; description: string; direction: 'MoneyIn' | 'MoneyOut'; amount: number }[];
  balanceMeaningNote: string | null;
}

export interface Row {
  id: string;
  bankAccountId: string;
  date: string;
  description: string;
  amount: number;
  balance: number | null;
  status: number;
  isExperimental: boolean;
  balanceCheckFailed: boolean;
}

export interface RowItem {
  row: Row;
  suggestedCategoryAccountId: string | null;
}

export interface ImportRequest {
  bankAccountId: string;
  bankName: string | null;
  layout?: Layout;
}

export interface PostRequest {
  categoryAccountId: string;
  allowDuplicate: boolean;
  reviewed: boolean;
}

const batchPath = (batchId: string) => `/imports/${batchId}`;

export const listBatches = () => api<Batch[]>('/imports');
/** The balances route lists only accounts that already have a posted entry. */
export const listAccounts = () => api<Account[]>('/ledger/balances');
export const listRows = (batchId: string) => api<RowItem[]>(`${batchPath(batchId)}/rows`);
export const saveProfile = (bankName: string, columns: CsvColumns) =>
  api('/imports/profiles', { method: 'PUT', body: { bankName, columns } });
export const previewLayout = (batchId: string, bankName: string, bankAccountId: string) =>
  api<Preview>(`${batchPath(batchId)}/layout-preview?${new URLSearchParams({ bankName, bankAccountId })}`);
export const importBatch = (batchId: string, body: ImportRequest) =>
  api(`${batchPath(batchId)}/import`, { method: 'POST', body });
export const postRow = (rowId: string, body: PostRequest) =>
  api(`/imports/rows/${rowId}/post`, { method: 'POST', body });
export const discardBatch = (batchId: string) => api(`${batchPath(batchId)}/discard`, { method: 'POST' });
