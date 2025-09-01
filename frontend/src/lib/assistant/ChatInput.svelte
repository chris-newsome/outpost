<script lang="ts">
  export let send: (message: string) => void;
  export let streaming = false;
  let text = '';
  function submit() {
    if (!text.trim() || streaming) return;
    const msg = text.trim();
    text = '';
    send(msg);
  }
</script>

<div class="chat-input">
  <input placeholder="Ask about tasks, bills, docs..." bind:value={text} on:keydown={(e) => e.key==='Enter' && submit()} />
  <button disabled={streaming || !text.trim()} on:click={submit}>Send</button>
</div>

<style>
  .chat-input { display:flex; gap:8px; }
  input { flex:1; padding:8px; border:1px solid #ddd; border-radius:6px; }
  button { padding:8px 12px; }
</style>

