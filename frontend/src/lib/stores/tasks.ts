import { writable } from 'svelte/store';
import type { Writable } from 'svelte/store';
import { apiFetch } from '$lib/api';

export interface TaskItem {
  id?: string;
  familyId?: string;
  title: string;
  description?: string;
  dueDate?: string | null;
  completed: boolean;
  assignedToUserId?: string | null;
  createdAt?: string;
  updatedAt?: string;
}

interface State {
  items: TaskItem[];
  loading: boolean;
}

function createStore() {
  const store: Writable<State> = writable<State>({ items: [], loading: false });
  const familyId = () => localStorage.getItem('familyId') || '00000000-0000-0000-0000-000000000001';

  async function load(): Promise<void> {
    store.update((s) => ({ ...s, loading: true }));
    try {
      const data = await apiFetch<TaskItem[]>(`/api/tasks?familyId=${familyId()}`);
      store.set({ items: data ?? [], loading: false });
    } catch (e) {
      store.update((s) => ({ ...s, loading: false }));
    }
  }

  async function create(task: Partial<TaskItem>): Promise<void> {
    const payload = { ...task, familyId: familyId(), completed: !!task.completed };
    await apiFetch<TaskItem>('/api/tasks', { method: 'POST', body: payload });
    await load();
  }

  async function toggle(task: TaskItem): Promise<void> {
    if (!task.id) return;
    await apiFetch<void>(`/api/tasks/${task.id}`, { method: 'PUT', body: { ...task, completed: !task.completed } });
    await load();
  }

  return {
    subscribe: store.subscribe,
    load,
    create,
    toggle
  };
}

export const tasksStore = createStore();
export type TasksStore = typeof tasksStore;

