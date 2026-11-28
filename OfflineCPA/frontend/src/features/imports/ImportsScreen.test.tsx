import { fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { route } from './route';
import { accounts, batch, beside, fakeBackend, rowItem } from './testing';

afterEach(() => vi.unstubAllGlobals());

const show = () => render(<route.component routes={[route]} />);
const openPdf = async () => {
  show();
  fireEvent.click(await screen.findByRole('button', { name: /Open PDF statement/ }));
  fireEvent.change(screen.getByLabelText('Bank account'), { target: { value: 'bank' } });
  return screen.getByRole('button', { name: 'Import' });
};

describe('Import screen', () => {
  it('registers the Import route', () => {
    expect(route).toMatchObject({ path: '/imports', label: 'Import' });
  });

  it('moves from the list to the review once the import succeeds', async () => {
    let status = 0;
    fakeBackend({
      'GET /imports': () => [batch({ extension: '.pdf', status })],
      'GET /ledger/balances': accounts,
      'POST /imports/b1/import': () => { status = 1; return { rows: 1 }; },
      'GET /imports/b1/rows': [rowItem('r1')],
    });

    fireEvent.click(await openPdf());
    expect(await screen.findByText(/Example Store r1/)).toBeTruthy();
  });

  it('shows the locked-books message as the server wrote it', async () => {
    fakeBackend({
      'GET /imports': [batch({ extension: '.pdf' })],
      'GET /ledger/balances': accounts,
      'POST /imports/b1/import': { status: 400, error: 'Unlock the books first.' },
    });

    const run = await openPdf();
    fireEvent.click(run);
    expect((await beside(run).findByRole('alert')).textContent).toBe('Unlock the books first.');
  });

  it('shows a list failure next to the Refresh button', async () => {
    fakeBackend({ 'GET /imports': { status: 500, error: 'The books could not be read.' }, 'GET /ledger/balances': [] });
    show();

    const refresh = screen.getByRole('button', { name: 'Refresh list' });
    expect((await beside(refresh).findByRole('alert')).textContent).toBe('The books could not be read.');
  });
});
