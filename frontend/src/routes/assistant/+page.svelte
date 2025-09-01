<script lang="ts">
  import { onMount } from 'svelte';
  import ChatInput from '../../lib/assistant/ChatInput.svelte';
  import ChatMessage from '../../lib/assistant/ChatMessage.svelte';
  import SourceChips from '../../lib/assistant/SourceChips.svelte';

  type Msg = { role: 'user'|'assistant'; content: string };
  let messages: Msg[] = [];
  let sessionId: string | null = null;
  let sources: { id: string; src: string }[] = [];
  let streaming = false;
  let consent = true; // TODO: fetch from API/user profile

  function loadLocal() {
    const raw = localStorage.getItem('assistant_messages');
    if (raw) messages = JSON.parse(raw);
    sessionId = localStorage.getItem('assistant_session');
  }
  function saveLocal() {
    localStorage.setItem('assistant_messages', JSON.stringify(messages));
    if (sessionId) localStorage.setItem('assistant_session', sessionId);
  }
  onMount(loadLocal);

  async function send(message: string) {
    if (!consent) {
      alert('AI features are disabled for your family.');
      return;
    }
    messages = [...messages, { role: 'user', content: message }];
    saveLocal();
    const API_BASE = (import.meta.env.VITE_API_BASE !== undefined ? (import.meta.env.VITE_API_BASE as string) : 'http://localhost:5000');
    const url = `${API_BASE}/api/assistant/chat`;
    const res = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ sessionId, message })
    });
    const reader = res.body?.getReader();
    if (!reader) return;
    streaming = true;
    let assistant = '';
    const decoder = new TextDecoder();
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      const chunk = decoder.decode(value);
      for (const line of chunk.split('\n')) {
        if (line.startsWith('data:')) {
          const data = line.slice(5).trim();
          if (data === '[DONE]') { break; }
          try {
            const obj = JSON.parse(data);
            if (obj.content) {
              assistant += obj.content;
              if (messages.length === 0 || messages[messages.length-1].role !== 'assistant') {
                messages = [...messages, { role: 'assistant', content: assistant }];
              } else {
                messages[messages.length-1].content = assistant;
                messages = [...messages];
              }
            }
            if (obj.sources) sources = obj.sources;
          } catch {}
        }
      }
    }
    streaming = false;
    saveLocal();
  }
</script>

<section class="assistant-container">
  <header class="toolbar">
    <h2>Assistant</h2>
    <label><input type="checkbox" bind:checked={consent}> Use my data</label>
  </header>
  <div class="messages">
    {#each messages as m, i}
      <ChatMessage {m} />
    {/each}
  </div>
  {#if sources.length}
    <SourceChips {sources} />
  {/if}
  <ChatInput {send} {streaming} />
</section>

<style>
  .assistant-container { display:flex; flex-direction:column; gap:8px; height:calc(100vh - 140px); }
  .messages { flex:1; overflow:auto; padding:8px; background:#fafafa; border:1px solid #eee; border-radius:8px; }
  .toolbar { display:flex; justify-content:space-between; align-items:center; }
</style>
