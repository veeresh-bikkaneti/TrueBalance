import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { route } from './route';

const OLD = 'fake passphrase for tests';
const NEW = 'another fake passphrase';
const CODE = '0000-1111-2222-3333-4444-5555-6666-7777';
const WRONG = 'That passphrase does not open the books.';

const states = {
  first: { unlocked: false, passphraseSet: false },
  locked: { unlocked: false, passphraseSet: true },
  open: { unlocked: true, passphraseSet: true },
};

/** A pretend backend. A good unlock or recovery opens the books. */
function backend(start: keyof typeof states, refuse = false) {
  let status = states[start];
  const fetch = vi.fn(async (url: string, init?: RequestInit) => {
    const reply = (body: unknown, code = 200) => new Response(JSON.stringify(body), { status: code });
    if (init?.method !== 'POST') return reply(status);
    if (refuse) return reply({ error: WRONG }, 400);
    if (url.endsWith('/unlock') || url.endsWith('/recover')) status = states.open;
    return reply(url.endsWith('/recovery-code') ? { recoveryCode: CODE } : {});
  });
  vi.stubGlobal('fetch', fetch);
  const posts = () => fetch.mock.calls.filter(([, init]) => init?.method === 'POST');
  return { posts };
}

async function show() {
  const Screen = route.component;
  render(<Screen routes={[route]} />);
  await screen.findByText(/the books are (un)?locked/i);
}

const form = (name: string) => within(screen.getByRole('form', { name }));

/** Types into each field of one form, presses its button, and hands back the inputs. */
function submit(name: string, button: string, values: Record<string, string>) {
  const inputs = Object.entries(values).map(([label, value]) => {
    const input = form(name).getByLabelText<HTMLInputElement>(label);
    fireEvent.change(input, { target: { value } });
    return input;
  });
  fireEvent.click(form(name).getByRole('button', { name: button }));
  return inputs;
}

const NEW_LABEL = 'New passphrase (at least 12 characters)';
const AGAIN_LABEL = 'Type the new passphrase again';
const pair = { [NEW_LABEL]: NEW, [AGAIN_LABEL]: NEW };

const forms = [
  { start: 'first', name: 'Set your passphrase', button: 'Set passphrase', values: pair, path: 'unlock', body: { passphrase: NEW } },
  { start: 'locked', name: 'Unlock the books', button: 'Unlock', values: { Passphrase: OLD }, path: 'unlock', body: { passphrase: OLD } },
  {
    start: 'locked', name: 'Forgot your passphrase?', button: 'Recover', values: { 'Recovery code': CODE, ...pair },
    path: 'recover', body: { recoveryCode: CODE, newPassphrase: NEW },
  },
  {
    start: 'open', name: 'Change your passphrase', button: 'Change passphrase', values: { 'Current passphrase': OLD, ...pair },
    path: 'change-passphrase', body: { currentPassphrase: OLD, newPassphrase: NEW },
  },
  { start: 'open', name: 'Make a recovery code', button: 'Make a code', values: { Passphrase: OLD }, path: 'recovery-code', body: { passphrase: OLD } },
] as const;

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe('vault screen', () => {
  it('is a screen the shell can find', () => {
    expect(route).toMatchObject({ path: '/vault', label: 'Vault' });
  });

  describe.each(forms)('$name', ({ start, name, button, values, path, body }) => {
    it.each([false, true])('sends the secrets in the body and clears every field (refused: %s)', async (refuse) => {
      const { posts } = backend(start, refuse);
      await show();

      const inputs = submit(name, button, values);

      await waitFor(() => expect(posts()).toHaveLength(1));
      const [url, init] = posts()[0];
      expect(url).toBe(`/api/vault/${path}`);
      expect(JSON.parse(String(init?.body))).toEqual(body);
      if (refuse) {
        const message = await screen.findByText(WRONG);
        expect(inputs[0].getAttribute('aria-describedby')).toBe(message.id);
      }
      await waitFor(() => expect(inputs.map((input) => input.value)).toEqual(inputs.map(() => '')));
    });
  });

  it.each([
    ['short', 'short', 'Use at least 12 characters.'],
    [NEW, OLD, 'The two passphrases do not match.'],
    ['', '', 'Fill in this field.'],
  ])('makes no call for a new passphrase of "%s" and "%s"', async (first, again, message) => {
    const { posts } = backend('first');
    await show();

    submit('Set your passphrase', 'Set passphrase', { [NEW_LABEL]: first, [AGAIN_LABEL]: again });

    expect(screen.getAllByText(message).length).toBeGreaterThan(0);
    expect(posts()).toHaveLength(0);
  });

  it('hides the recovery code after the tick and keeps it nowhere', async () => {
    backend('open');
    const stored = vi.spyOn(Storage.prototype, 'setItem');
    const logs = (['log', 'info', 'warn', 'error', 'debug'] as const).map((level) => vi.spyOn(console, level));
    await show();
    submit('Make a recovery code', 'Make a code', { Passphrase: OLD });
    await screen.findByText(CODE);

    fireEvent.click(screen.getByRole('checkbox', { name: /I wrote it down/ }));

    expect(screen.queryByText(CODE)).toBeNull();
    expect(document.body.innerHTML).not.toContain(CODE);
    expect(JSON.stringify([localStorage, sessionStorage, document.cookie, window.location.href])).not.toContain(CODE);
    expect(stored).not.toHaveBeenCalled();
    logs.forEach((log) => expect(log).not.toHaveBeenCalled());
  });

  it('unlocks the books and then offers a recovery code', async () => {
    backend('locked');
    await show();

    submit('Unlock the books', 'Unlock', { Passphrase: OLD });

    await screen.findByText('The books are unlocked.');
    expect(screen.getByRole('form', { name: 'Make a recovery code' })).toBeTruthy();
  });

  it('asks for a new code after a recovery', async () => {
    backend('locked');
    await show();

    submit('Forgot your passphrase?', 'Recover', { 'Recovery code': CODE, ...pair });

    await screen.findByText(/That code is used up, so make a new one now/);
    submit('Make a recovery code', 'Make a code', { Passphrase: NEW });
    await screen.findByText(CODE);
    expect(screen.queryByText(/That code is used up/)).toBeNull();
  });

  it('offers to try again when the status cannot be read', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValueOnce(new TypeError('offline'))
      .mockResolvedValue(new Response(JSON.stringify(states.locked))));
    const Screen = route.component;
    render(<Screen routes={[route]} />);

    fireEvent.click(await screen.findByRole('button', { name: 'Try again' }));

    await screen.findByText('The books are locked.');
  });
});
