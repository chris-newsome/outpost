<script lang="ts">
  export let sources: { id: string; src: string }[] = [];
</script>

<div class="chips">
  {#each sources as s}
    <span class="chip">{s.src} #{s.id.slice(0,6)}</span>
  {/each}
</div>

<style>
  .chips { display:flex; gap:6px; flex-wrap:wrap; margin:6px 0; }
  .chip { background:#eef3ff; color:#3355aa; border:1px solid #d8e2ff; padding:4px 8px; border-radius:999px; font-size:12px; }
</style>

