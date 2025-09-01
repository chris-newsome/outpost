<script lang="ts">
  export let m: { role: 'user'|'assistant', content: string };
</script>

<div class="msg {m.role}">
  <div class="role">{m.role}</div>
  <div class="bubble">{m.content}</div>
  </div>

<style>
  .msg { display:flex; align-items:flex-start; gap:8px; margin:6px 0; }
  .msg.user { flex-direction: row-reverse; }
  .role { font-size:12px; color:#777; width:70px; text-align:right; }
  .msg.user .role { text-align:left; }
  .bubble { background:white; border:1px solid #eee; border-radius:10px; padding:8px 10px; max-width:70%; white-space:pre-wrap; }
</style>

