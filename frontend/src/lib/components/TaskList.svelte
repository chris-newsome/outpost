<script lang="ts">
  import type { TasksStore } from '$lib/stores/tasks';
  export let tasksStore: TasksStore;
  let title: string = '';

  async function add() {
    if (title.trim().length === 0) return;
    await tasksStore.create({ title, description: '', completed: false });
    title = '';
  }
</script>

<div style="display:grid; gap:0.5rem; max-width:600px">
  <div style="display:flex; gap:0.5rem">
    <input placeholder="New task" bind:value={title} />
    <button on:click={add}>Add</button>
  </div>

  {#if $tasksStore.loading}
    <p>Loading…</p>
  {:else}
    <ul>
      {#each $tasksStore.items as t}
        <li>
          <input type="checkbox" checked={t.completed} on:change={() => tasksStore.toggle(t)} />
          {t.title}
        </li>
      {/each}
    </ul>
  {/if}
</div>

