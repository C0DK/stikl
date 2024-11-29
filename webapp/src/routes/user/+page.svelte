<script lang="ts">
	import type { PageData } from './$types';
	import UserCard from './UserCard.svelte';

	let { data }: { data: PageData } = $props();
</script>

<h1 class="text-3xl font-bold">All users:</h1>
<div class="flex gap-5 pl-10 pt-5">
	{#each data.users as user}
		<UserCard {user} />
	{/each}
</div>
