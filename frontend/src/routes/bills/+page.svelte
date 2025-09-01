<script lang="ts">
  import { billsStore } from '$lib/stores/bills';
  import { onMount } from 'svelte';
  onMount(() => billsStore.load());
</script>

<h1>Bills</h1>
{#if $billsStore.loading}
  <p>Loading…</p>
{:else}
  <table>
    <thead>
      <tr><th>Vendor</th><th>Amount</th><th>Due</th><th>Status</th></tr>
    </thead>
    <tbody>
    {#each $billsStore.items as b}
      <tr>
        <td>{b.vendor}</td>
        <td>${b.amount.toFixed(2)}</td>
        <td>{new Date(b.dueDate).toLocaleDateString()}</td>
        <td>{b.status}</td>
      </tr>
    {/each}
    </tbody>
  </table>
{/if}

