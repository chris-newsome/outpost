<script lang="ts">
  import { tasksStore } from '$lib/stores/tasks';
  import { billsStore } from '$lib/stores/bills';
  import { onMount } from 'svelte';

  onMount(() => {
    // Load sample data on dashboard
    tasksStore.load();
    billsStore.load();
  });
</script>

<h1>Dashboard</h1>

<section>
  <h2>Tasks (glance)</h2>
  {#if $tasksStore.loading}
    <p>Loading tasks…</p>
  {:else}
    <ul>
      {#each $tasksStore.items.slice(0,5) as t}
        <li>{t.title} {#if t.dueDate}— due {new Date(t.dueDate).toLocaleDateString()}{/if}</li>
      {/each}
    </ul>
  {/if}
</section>

<section>
  <h2>Bills (glance)</h2>
  {#if $billsStore.loading}
    <p>Loading bills…</p>
  {:else}
    <ul>
      {#each $billsStore.items.slice(0,5) as b}
        <li>{b.vendor}: ${b.amount} — due {new Date(b.dueDate).toLocaleDateString()}</li>
      {/each}
    </ul>
  {/if}
</section>

