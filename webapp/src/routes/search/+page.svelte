<script lang="ts">
	import SearchBar from '$lib/components/Searchbar.svelte';
	import type { PageData } from './$types';
	import PlantCard from './PlantCard.svelte';

	let { data }: { data: PageData } = $props();
</script>

<SearchBar query={data.query} />
<div class="grid grid-cols-3 gap-4">
	{#if data.plants.length === 0}
		<h2 class="text-xl">Vi kunne ikke finde nogle planter baseret på din søgning...</h2>
	{/if}
	{#each data.plants as plant}
		<PlantCard {plant} />
	{/each}
</div>
