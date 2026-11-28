/** A failed call. `message` is the server's `error` text when it sent one. */
export class ApiError extends Error {
  constructor(message: string, readonly status: number) {
    super(message);
    this.name = 'ApiError';
  }
}

/**
 * The only place the app calls `fetch`. JSON goes in, JSON comes out.
 * `path` starts with a slash and hangs off `VITE_API_URL` (default `/api`).
 */
export async function api<T>(path: string, options: { method?: string; body?: unknown } = {}): Promise<T> {
  const base = import.meta.env.VITE_API_URL || '/api';
  const hasBody = options.body !== undefined;

  const response = await fetch(`${base}${path}`, {
    method: options.method ?? 'GET',
    headers: hasBody ? { 'Content-Type': 'application/json' } : {},
    body: hasBody ? JSON.stringify(options.body) : undefined,
  });
  const text = await response.text();

  let data: unknown;
  let isJson = true;
  try {
    data = text ? JSON.parse(text) : undefined;
  } catch {
    isJson = false;
  }

  if (!response.ok || !isJson) {
    const serverText = (data as { error?: unknown } | null | undefined)?.error;
    throw new ApiError(
      typeof serverText === 'string' ? serverText : `Request failed (${response.status})`,
      response.status,
    );
  }
  return data as T;
}
