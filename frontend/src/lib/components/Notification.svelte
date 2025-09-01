<script lang="ts">
  export let message: string = '';
  export let type: 'info' | 'success' | 'error' = 'info';
</script>

<div class={`notice ${type}`} role="status">{message}</div>

<style>
  .notice { padding: 0.5rem 0.75rem; border-radius: 6px; }
  .info { background: #eff6ff; color: #1e40af; }
  .success { background: #ecfdf5; color: #065f46; }
  .error { background: #fef2f2; color: #991b1b; }
  </style>

