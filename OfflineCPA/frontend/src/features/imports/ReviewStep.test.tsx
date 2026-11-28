import { fireEvent, render, screen, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ReviewStep } from './ReviewStep';
import { accounts, batch, beside, fakeBackend, rowItem } from './testing';

afterEach(() => vi.unstubAllGlobals());

const show = (onDiscarded = vi.fn()) =>
  render(<ReviewStep batch={batch({ status: 1 })} accounts={accounts} onDiscarded={onDiscarded} />);
const rowOf = async (text: string) => (await screen.findByText(new RegExp(text))).closest('li') as HTMLElement;
const discardButton = () => screen.queryByRole('button', { name: 'Discard this import' });

describe('ReviewStep', () => {
  it('blocks posting a duplicate until the extra tick is set', async () => {
    const calls = fakeBackend({
      'GET /imports/b1/rows': [rowItem('r1', { status: 1 })],
      'POST /imports/rows/r1/post': { id: 'e1' },
    });
    show();
    const row = within(await rowOf('Example Store r1'));
    const post = row.getByRole('button', { name: 'Post' });

    expect(post).toHaveProperty('disabled', true);
    fireEvent.click(post);
    expect(calls.filter((c) => c.key.startsWith('POST'))).toEqual([]);

    fireEvent.click(row.getByLabelText(/Post it anyway/));
    fireEvent.click(post);
    await vi.waitFor(() => expect(calls).toContainEqual({
      key: 'POST /imports/rows/r1/post',
      body: { categoryAccountId: 'supplies', allowDuplicate: true, reviewed: false },
    }));
  });

  it('needs a category, and a check tick for an experimental row', async () => {
    const calls = fakeBackend({
      'GET /imports/b1/rows': [rowItem('r1', { isExperimental: true }, null)],
      'POST /imports/rows/r1/post': { id: 'e1' },
    });
    show();
    const row = within(await rowOf('Example Store r1'));
    const post = row.getByRole('button', { name: 'Post' });

    fireEvent.click(row.getByLabelText(/I checked this row/));
    expect(post).toHaveProperty('disabled', true);
    fireEvent.change(row.getByLabelText('Category'), { target: { value: 'supplies' } });
    fireEvent.click(post);
    await vi.waitFor(() => expect(calls).toContainEqual({
      key: 'POST /imports/rows/r1/post',
      body: { categoryAccountId: 'supplies', allowDuplicate: false, reviewed: true },
    }));
  });

  it("shows the server's error next to the row that failed", async () => {
    fakeBackend({
      'GET /imports/b1/rows': [rowItem('r1'), rowItem('r2')],
      'POST /imports/rows/r2/post': { status: 400, error: 'The period is locked.' },
    });
    show();
    const post = within(await rowOf('Example Store r2')).getByRole('button', { name: 'Post' });

    fireEvent.click(post);
    expect((await beside(post).findByRole('alert')).textContent).toBe('The period is locked.');
    expect(screen.getAllByRole('alert')).toHaveLength(1);
  });

  it('discards only after a confirm', async () => {
    const calls = fakeBackend({
      'GET /imports/b1/rows': [rowItem('r1')],
      'POST /imports/b1/discard': { discarded: 1 },
    });
    const confirm = vi.fn().mockReturnValueOnce(false).mockReturnValueOnce(true);
    vi.stubGlobal('confirm', confirm);
    const onDiscarded = vi.fn();
    show(onDiscarded);
    const discard = await screen.findByRole('button', { name: 'Discard this import' });

    fireEvent.click(discard);
    expect(confirm).toHaveBeenCalledTimes(1);
    expect(calls.map((c) => c.key)).not.toContain('POST /imports/b1/discard');
    await vi.waitFor(() => expect(discard).toHaveProperty('disabled', false));

    fireEvent.click(discard);
    await vi.waitFor(() => expect(onDiscarded).toHaveBeenCalled());
    expect(calls.map((c) => c.key)).toContain('POST /imports/b1/discard');
  });

  it('hides discard once any row is posted', async () => {
    let posted = false;
    fakeBackend({
      'GET /imports/b1/rows': () => [rowItem('r1', { status: posted ? 2 : 0 }), rowItem('r2')],
      'POST /imports/rows/r1/post': () => { posted = true; return { id: 'e1' }; },
    });
    show();
    const row = within(await rowOf('Example Store r1'));
    expect(discardButton()).not.toBeNull();

    fireEvent.click(row.getByRole('button', { name: 'Post' }));
    expect(await row.findByText('Posted.')).toBeTruthy();
    expect(discardButton()).toBeNull();
  });
});
