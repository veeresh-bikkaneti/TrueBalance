import { afterEach, describe, expect, it, vi } from 'vitest';
import { api, ApiError } from './api';

const respond = (body: string, status = 200) => {
  const fetchMock = vi.fn().mockResolvedValue(new Response(body || null, { status }));
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
};

afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
});

describe('api', () => {
  it('prefixes VITE_API_URL and returns the JSON', async () => {
    vi.stubEnv('VITE_API_URL', 'http://books.test/api');
    const fetchMock = respond('{"ok":true}');

    await expect(api('/accounts')).resolves.toEqual({ ok: true });
    expect(fetchMock.mock.calls[0][0]).toBe('http://books.test/api/accounts');
  });

  it('uses /api when VITE_API_URL is not set', async () => {
    vi.stubEnv('VITE_API_URL', '');
    const fetchMock = respond('[]');

    await api('/accounts');
    expect(fetchMock.mock.calls[0][0]).toBe('/api/accounts');
  });

  it('sends a body as JSON', async () => {
    const fetchMock = respond('{}');

    await api('/books/setup', { method: 'POST', body: { kind: 'household' } });
    const init = fetchMock.mock.calls[0][1];
    expect(init.method).toBe('POST');
    expect(init.headers['Content-Type']).toBe('application/json');
    expect(init.body).toBe('{"kind":"household"}');
  });

  it('returns undefined for an empty reply', async () => {
    respond('', 204);

    await expect(api('/lock', { method: 'POST' })).resolves.toBeUndefined();
  });

  it("turns an error body into an ApiError with the server's text", async () => {
    respond('{"error":"The period is locked."}', 409);

    const failure = await api('/entries').catch((e: unknown) => e);
    expect(failure).toBeInstanceOf(ApiError);
    expect(failure).toMatchObject({ message: 'The period is locked.', status: 409 });
  });

  it('still gives an ApiError when the reply is not JSON', async () => {
    respond('<html>Bad gateway</html>', 502);
    await expect(api('/entries')).rejects.toMatchObject({ name: 'ApiError', message: 'Request failed (502)', status: 502 });

    respond('<html>oops</html>', 200);
    await expect(api('/entries')).rejects.toMatchObject({ name: 'ApiError', status: 200 });
  });
});
