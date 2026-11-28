// Test helpers only: a fake backend that answers by "METHOD /path".
import { within } from '@testing-library/react';
import { vi } from 'vitest';

export function fakeBackend(replies: Record<string, unknown>) {
  const calls: { key: string; body: unknown }[] = [];
  vi.stubGlobal('fetch', vi.fn(async (url: string, init: RequestInit) => {
    const key = `${init.method} ${url.replace('/api', '').split('?')[0]}`;
    calls.push({ key, body: init.body ? JSON.parse(init.body as string) : undefined });
    if (!(key in replies)) throw new Error(`No fake reply for ${key}`);
    // A function builds a fresh reply for each call.
    const entry = replies[key];
    const reply = (typeof entry === 'function' ? entry() : entry) as { status?: number; error?: string };
    const status = typeof reply.error === 'string' ? reply.status : 200;
    return new Response(JSON.stringify(reply), { status });
  }));
  return calls;
}

/** Queries scoped to the block that holds a button, so "next to the action" is checked for real. */
export const beside = (button: HTMLElement) => within(button.closest('.action') as HTMLElement);

export const accounts = [
  { accountId: 'bank', code: '1000', name: 'Example Checking' },
  { accountId: 'supplies', code: '5000', name: 'Supplies' },
];

export const batch = (over: object = {}) => ({
  id: 'b1', extension: '.csv', status: 0, failureReason: null, balanceReason: null,
  createdAtUtc: '2026-01-07T10:00:00Z', ...over,
});

export const rowItem = (id: string, over: object = {}, suggested: string | null = 'supplies') => ({
  row: {
    id, bankAccountId: 'bank', date: '2026-01-05', description: `Example Store ${id}`, amount: -12.5, balance: 987.5,
    status: 0, isExperimental: false, balanceCheckFailed: false, ...over,
  },
  suggestedCategoryAccountId: suggested,
});
