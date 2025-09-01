<script lang="ts">
  import TaskList from '$lib/components/TaskList.svelte';
  import { tasksStore } from '$lib/stores/tasks';
  import { onMount } from 'svelte';
  onMount(() => tasksStore.load());
</script>

<h1>Tasks</h1>
<TaskList {tasksStore} />

