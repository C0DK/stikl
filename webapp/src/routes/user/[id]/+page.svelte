<script lang="ts">
	import type { PageData } from './$types';
	import PlantCard from './plant-card.svelte';

	let { data }: { data: PageData } = $props();
</script>

<h1>{data.name}</h1>

<h2>Har følgende planter:</h2>
{#each data.has as plant}
	<PlantCard {plant} />
{/each}
<h2>Ønsker sig:</h2>
{#each data.needs as plant}
	<PlantCard {plant} />
{/each}
