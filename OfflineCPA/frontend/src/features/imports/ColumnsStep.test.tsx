import { fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ColumnsStep } from './ColumnsStep';
import { accounts, batch, beside, fakeBackend } from './testing';

afterEach(() => vi.unstubAllGlobals());

const type = (label: string, value: string) => fireEvent.change(screen.getByLabelText(label), { target: { value } });
const click = (name: string) => fireEvent.click(screen.getByRole('button', { name }));
const fill = () => { type('Bank account', 'bank'); type('Bank name', 'Example Bank'); };
const preview = {
  layout: {
    amountLayout: 'Signed', debitsCarryMinus: false, indicatorColumn: null, moneyInMarker: null,
    moneyOutMarker: null, balanceMeaning: 0,
  },
  balanceCheck: 'Passed',
  rows: [{ date: '2026-01-05', description: 'Example Store', direction: 'MoneyOut', amount: 12.5 }],
  balanceMeaningNote: null,
};

describe('ColumnsStep', () => {
  it("shows the server's refusal next to the saved-settings button only", async () => {
    fakeBackend({ 'POST /imports/b1/import': { status: 400, error: 'Confirm how this bank signs amounts first.' } });
    render(<ColumnsStep batch={batch()} accounts={accounts} onImported={vi.fn()} />);
    fill();

    const saved = screen.getByRole('button', { name: 'Import with saved bank settings' });
    fireEvent.click(saved);
    expect((await beside(saved).findByRole('alert')).textContent).toBe('Confirm how this bank signs amounts first.');
    expect(screen.getAllByRole('alert')).toHaveLength(1);
  });

  it('saves the columns, previews, and imports with the confirmed sign', async () => {
    const calls = fakeBackend({
      'PUT /imports/profiles': { saved: true },
      'GET /imports/b1/layout-preview': preview,
      'POST /imports/b1/import': { rows: 1 },
    });
    const onImported = vi.fn();
    render(<ColumnsStep batch={batch()} accounts={accounts} onImported={onImported} />);
    fill();
    type('Description column', 'memo');
    click('Save columns and preview');
    expect(await screen.findByText(/Example Store/)).toBeTruthy();
    expect(calls[0].body).toEqual({
      bankName: 'Example Bank',
      columns: {
        date: 'date', description: 'memo', amount: 'amount', debit: null, credit: null, balance: 'balance',
        dateFormat: null, amountLayout: 'Signed', debitsCarryMinus: false, indicatorColumn: null,
        moneyInMarker: null, moneyOutMarker: null,
      },
    });

    type("Bank's sign", 'SignedReversed');
    type('The balance column counts', '2');
    click('Confirm and import');
    await vi.waitFor(() => expect(onImported).toHaveBeenCalled());
    expect(calls[calls.length - 1].body).toEqual({
      bankAccountId: 'bank', bankName: 'Example Bank',
      layout: { ...preview.layout, amountLayout: 'SignedReversed', balanceMeaning: 2 },
    });
  });

  it('sends debit and credit columns, and drops the preview when a column changes', async () => {
    const calls = fakeBackend({
      'PUT /imports/profiles': { saved: true },
      'GET /imports/b1/layout-preview': { ...preview, layout: { ...preview.layout, amountLayout: 'DebitCredit' } },
    });
    render(<ColumnsStep batch={batch()} accounts={accounts} onImported={vi.fn()} />);
    fill();
    type('Amounts are in', 'DebitCredit');
    click('Save columns and preview');
    await screen.findByRole('button', { name: 'Confirm and import' });
    expect(calls[0].body).toMatchObject({ columns: { amount: null, debit: 'debit', credit: 'credit' } });

    type('Debit column', 'withdrawals');
    expect(screen.queryByRole('button', { name: 'Confirm and import' })).toBeNull();
  });

  it('asks a PDF only for the account', async () => {
    const calls = fakeBackend({ 'POST /imports/b1/import': { rows: 1 } });
    render(<ColumnsStep batch={batch({ extension: '.pdf' })} accounts={accounts} onImported={vi.fn()} />);
    expect(screen.queryByLabelText('Bank name')).toBeNull();
    type('Bank account', 'bank');
    click('Import');
    await vi.waitFor(() => expect(calls).toContainEqual({
      key: 'POST /imports/b1/import', body: { bankAccountId: 'bank', bankName: null },
    }));
  });
});
