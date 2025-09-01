<script lang="ts">
  import { documentsStore } from '$lib/stores/documents';
  import { onMount } from 'svelte';
  let file: File | null = null;
  onMount(() => documentsStore.load());

  async function upload() {
    if (file) await documentsStore.upload(file);
  }
</script>

<h1>Documents</h1>

<div style="margin:1rem 0">
  <input type="file" bind:files={file} on:change={(e: any) => file = e?.target?.files?.[0] ?? null} />
  <button on:click={upload} disabled={!file}>Upload</button>
  {#if $documentsStore.uploading}<span>Uploading…</span>{/if}
  {#if $documentsStore.error}<span style="color:#be123c">{$documentsStore.error}</span>{/if}
  {#if $documentsStore.message}<span style="color:#16a34a">{$documentsStore.message}</span>{/if}
  </div>

{#if $documentsStore.loading}
  <p>Loading…</p>
{:else}
  <ul>
    {#each $documentsStore.items as d}
      <li>
        {d.name} — <a href={d.signedUrl} target="_blank" rel="noreferrer">Open</a>
      </li>
    {/each}
  </ul>
{/if}

