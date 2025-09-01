import { writable } from 'svelte/store';
import type { Writable } from 'svelte/store';
import { apiFetch } from '$lib/api';

export interface AuthState {
  user: { email: string } | null;
  accessToken: string | null;
  refreshToken: string | null;
}

function createAuth() {
  const initial: AuthState = {
    user: null,
    accessToken: null,
    refreshToken: null
  };

  const stored = typeof localStorage !== 'undefined' ? localStorage.getItem('auth') : null;
  const start: AuthState = stored ? JSON.parse(stored) : initial;
  const store: Writable<AuthState> = writable<AuthState>(start);

  store.subscribe((val) => {
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('auth', JSON.stringify(val));
    }
  });

  async function login(email: string, password: string): Promise<void> {
    const res = await apiFetch<{ accessToken: string; refreshToken: string }>(
      '/api/auth/login',
      { method: 'POST', body: { email, password }, retryOffline: false }
    );
    store.update((s) => ({ ...s, user: { email }, accessToken: res?.accessToken ?? null, refreshToken: res?.refreshToken ?? null }));
  }

  async function logout(): Promise<void> {
    store.set(initial);
  }

  async function refresh(): Promise<void> {
    const current = get();
    if (!current.refreshToken) return;
    const res = await apiFetch<{ accessToken: string; refreshToken: string }>(
      '/api/auth/refresh',
      { method: 'POST', body: { refreshToken: current.refreshToken }, retryOffline: false }
    );
    store.update((s) => ({ ...s, accessToken: res.accessToken, refreshToken: res.refreshToken }));
  }

  function get(): AuthState {
    let val: AuthState;
    const unsub = store.subscribe((v) => (val = v));
    unsub();
    // @ts-expect-error guaranteed set above
    return val;
  }

  return {
    subscribe: store.subscribe,
    login,
    logout,
    refresh,
    get
  };
}

export const auth = createAuth();
export type AuthStore = typeof auth;

