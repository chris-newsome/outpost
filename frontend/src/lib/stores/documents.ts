import { writable } from 'svelte/store';
import type { Writable } from 'svelte/store';
import { apiFetch } from '$lib/api';

export interface DocumentItem {
  id: string;
  familyId: string;
  name: string;
  contentType?: string | null;
  storagePath: string;
  signedUrl?: string;
  createdAt?: string;
}

interface State {
  items: DocumentItem[];
  loading: boolean;
  uploading: boolean;
  error: string | null;
  message: string | null;
}

function createStore() {
  const store: Writable<State> = writable<State>({ items: [], loading: false, uploading: false, error: null, message: null });
  const familyId = () => localStorage.getItem('familyId') || '00000000-0000-0000-0000-000000000001';

  async function load(): Promise<void> {
    store.update((s) => ({ ...s, loading: true }));
    try {
      const docs = await apiFetch<DocumentItem[]>(`/api/documents?familyId=${familyId()}`);
      const enriched = await Promise.all(
        (docs ?? []).map(async (d) => {
          const signed = await apiFetch<{ url: string }>(`/api/documents/${d.id}/signed-url`);
          return { ...d, signedUrl: signed?.url };
        })
      );
      store.set({ items: enriched, loading: false, uploading: false, error: null, message: null });
    } catch (e: any) {
      store.update((s) => ({ ...s, loading: false, error: e?.message ?? 'Failed to load' }));
    }
  }

  async function upload(file: File): Promise<void> {
    store.update((s) => ({ ...s, uploading: true, error: null, message: null }));
    try {
      const form = new FormData();
      form.append('familyId', familyId());
      form.append('file', file);
      const res = await fetch(`${import.meta.env.VITE_API_BASE || 'http://localhost:5000'}/api/documents/upload`, {
        method: 'POST',
        body: form
      });
      if (!res.ok) throw new Error('Upload failed');
      store.update((s) => ({ ...s, uploading: false, message: 'Uploaded' }));
      await load();
    } catch (e: any) {
      store.update((s) => ({ ...s, uploading: false, error: e?.message ?? 'Upload failed' }));
    }
  }

  return { subscribe: store.subscribe, load, upload };
}

export const documentsStore = createStore();
export type DocumentsStore = typeof documentsStore;

