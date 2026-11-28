import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  discardBatch, importBatch, listAccounts, listBatches, listRows, postRow, previewLayout, saveProfile,
  type CsvColumns,
} from './api';

const fakeFetch = () => {
  const mock = vi.fn().mockImplementation(async () => new Response('{}', { status: 200 }));
  vi.stubGlobal('fetch', mock);
  return mock;
};

afterEach(() => vi.unstubAllGlobals());

const columns: CsvColumns = {
  date: 'date', description: 'memo', amount: null, debit: 'out', credit: 'in', balance: null, dateFormat: null,
  amountLayout: 'DebitCredit', debitsCarryMinus: false, indicatorColumn: null, moneyInMarker: null, moneyOutMarker: null,
};
const layout = {
  amountLayout: 'Signed' as const, debitsCarryMinus: false, indicatorColumn: null,
  moneyInMarker: null, moneyOutMarker: null, balanceMeaning: 1 as const,
};

describe('imports api', () => {
  it.each([
    ['listBatches', () => listBatches(), 'GET', '/api/imports', undefined],
    ['listAccounts', () => listAccounts(), 'GET', '/api/ledger/balances', undefined],
    ['listRows', () => listRows('b1'), 'GET', '/api/imports/b1/rows', undefined],
    ['saveProfile', () => saveProfile('Example Bank', columns), 'PUT', '/api/imports/profiles',
      { bankName: 'Example Bank', columns }],
    ['previewLayout', () => previewLayout('b1', 'A & B Bank', 'a1'), 'GET',
      '/api/imports/b1/layout-preview?bankName=A+%26+B+Bank&bankAccountId=a1', undefined],
    ['importBatch', () => importBatch('b1', { bankAccountId: 'a1', bankName: 'Example Bank', layout }), 'POST',
      '/api/imports/b1/import', { bankAccountId: 'a1', bankName: 'Example Bank', layout }],
    ['postRow', () => postRow('r1', { categoryAccountId: 'a2', allowDuplicate: false, reviewed: true }), 'POST',
      '/api/imports/rows/r1/post', { categoryAccountId: 'a2', allowDuplicate: false, reviewed: true }],
    ['discardBatch', () => discardBatch('b1'), 'POST', '/api/imports/b1/discard', undefined],
  ])('%s calls the right route with the right body', async (_name, call, method, url, body) => {
    const mock = fakeFetch();

    await call();
    expect(mock).toHaveBeenCalledTimes(1);
    const [sentUrl, init] = mock.mock.calls[0];
    expect(sentUrl).toBe(url);
    expect(init.method).toBe(method);
    expect(init.body === undefined ? undefined : JSON.parse(init.body)).toEqual(body);
  });
});
