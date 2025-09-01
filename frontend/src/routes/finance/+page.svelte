<script lang="ts">
  import { apiFetch } from '$lib/api';
  let linkToken: string | null = null;
  let familyId: string = localStorage.getItem('familyId') || '00000000-0000-0000-0000-000000000001';

  async function createLinkToken() {
    const res = await apiFetch<{ linkToken: string }>(`/api/finance/link-token`, { method: 'POST', body: { familyId } });
    linkToken = res?.linkToken ?? null;
  }
</script>

<h1>Finance</h1>
<button on:click={createLinkToken}>Create Link Token</button>
{#if linkToken}
  <p>Link Token: {linkToken}</p>
  <p>Integrate with Plaid Link widget here.</p>
{/if}

