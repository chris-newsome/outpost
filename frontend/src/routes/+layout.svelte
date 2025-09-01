<script lang="ts">
  import { onMount } from 'svelte';
  import { auth } from '$lib/stores/auth';

  let deferredPrompt: Event | null = null;

  onMount(() => {
    window.addEventListener('beforeinstallprompt', (e: Event) => {
      e.preventDefault();
      deferredPrompt = e;
    });

    if ('serviceWorker' in navigator) {
      navigator.serviceWorker.register('/service-worker.js');
    }
  });

  function install() {
    (deferredPrompt as any)?.prompt?.();
  }
</script>

<nav style="display:flex; gap:1rem; padding:1rem; background:#f1f5f9">
  <a href="/">Dashboard</a>
  <a href="/tasks">Tasks</a>
  <a href="/bills">Bills</a>
  <a href="/documents">Documents</a>
  <a href="/login" style="margin-left:auto">{ $auth.user ? 'Account' : 'Login' }</a>
  <button on:click={install} aria-label="Install app">Install</button>
  {#if $auth.user}
    <button on:click={() => auth.logout()}>Logout</button>
  {/if}
</nav>

<slot />

<style>
  nav a { text-decoration: none; color: #0f172a; }
  nav a:hover { text-decoration: underline; }
</style>

