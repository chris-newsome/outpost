<script lang="ts">
  import { auth } from '$lib/stores/auth';
  import { goto } from '$app/navigation';

  let email: string = '';
  let password: string = '';
  let error: string = '';

  async function submit() {
    error = '';
    try {
      await auth.login(email, password);
      goto('/');
    } catch (e: any) {
      error = e?.message ?? 'Login failed';
    }
  }
</script>

<h1>Login</h1>

{#if error}
  <p style="color:#be123c">{error}</p>
{/if}

<form on:submit|preventDefault={submit} style="display:grid; gap:0.5rem; max-width:320px">
  <label>
    <span>Email</span>
    <input type="email" bind:value={email} required />
  </label>
  <label>
    <span>Password</span>
    <input type="password" bind:value={password} required />
  </label>
  <button type="submit">Sign in</button>
</form>

<p>Or sign in with Apple (stub)</p>
<button on:click={() => alert('Sign in with Apple stub')}>Sign in with Apple</button>

