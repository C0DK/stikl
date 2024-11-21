<script lang="ts">
	import type { User } from '$lib/types';
	import type { PageData } from './$types';
	import PlantCard from './PlantCard.svelte';

	let { data }: { data: PageData } = $props();
	let user: User = data.user;
</script>

		<h2 class="pb-5 pt-0 font-sans text-xl font-bold text-slate-600">{user.firstName} giver:</h2>
		<div class="grid grid-cols-3 gap-4">
			{#each user.has as plant}
				<PlantCard {plant} buttonLabel="Ja tak" />
			{/each}
		</div>
</div>
