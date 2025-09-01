import { get } from 'svelte/store';
import { auth } from '$lib/stores/auth';

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

export interface ApiOptions {
  method?: HttpMethod;
  body?: unknown;
  headers?: Record<string, string>;
  retryOffline?: boolean; // default true for non-GET
}

export interface QueuedRequest {
  id: string;
  url: string;
  method: HttpMethod;
  headers: Record<string, string>;
  body?: unknown;
}

const API_BASE = import.meta.env.VITE_API_BASE || 'http://localhost:5000';

export async function apiFetch<T>(path: string, opts: ApiOptions = {}): Promise<T> {
  const method = opts.method ?? 'GET';
  const token = get(auth).accessToken;
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(opts.headers ?? {})
  };
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const url = path.startsWith('http') ? path : `${API_BASE}${path}`;
  const init: RequestInit = {
    method,
    headers,
    body: method === 'GET' ? undefined : JSON.stringify(opts.body)
  };

  try {
    const res = await fetch(url, init);
    if (!res.ok) throw new Error(`API ${res.status}`);
    return (await res.json()) as T;
  } catch (err) {
    const retryOffline = opts.retryOffline ?? method !== 'GET';
    if (retryOffline && !navigator.onLine) {
      await queueRequest({
        id: crypto.randomUUID(),
        url,
        method,
        headers,
        body: opts.body
      });
      // optimistic response for writes can be handled by caller
      return Promise.resolve(undefined as T);
    }
    throw err;
  }
}

async function queueRequest(req: QueuedRequest): Promise<void> {
  const reg = await navigator.serviceWorker.ready;
  reg.active?.postMessage({ type: 'QUEUE_REQUEST', payload: req });
}

