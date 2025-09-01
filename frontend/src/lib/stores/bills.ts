import { writable } from 'svelte/store';
import type { Writable } from 'svelte/store';
import { apiFetch } from '$lib/api';

export interface Bill {
  id?: string;
  familyId?: string;
  vendor: string;
  amount: number;
  dueDate: string;
  status: string;
  category?: string | null;
  recurring?: boolean;
}

interface State {
  items: Bill[];
  loading: boolean;
}

function createStore() {
  const store: Writable<State> = writable<State>({ items: [], loading: false });
  const familyId = () => localStorage.getItem('familyId') || '00000000-0000-0000-0000-000000000001';

  async function load(): Promise<void> {
    store.update((s) => ({ ...s, loading: true }));
    try {
      const data = await apiFetch<Bill[]>(`/api/bills?familyId=${familyId()}`);
      store.set({ items: data ?? [], loading: false });
    } catch {
      store.update((s) => ({ ...s, loading: false }));
    }
  }

  return {
    subscribe: store.subscribe,
    load
  };
}

export const billsStore = createStore();
export type BillsStore = typeof billsStore;

