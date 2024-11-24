<script lang="ts">
	import type { PlantKind } from '$lib/types';
	import type { PageData } from './$types';
	import PlantCard from './SeedCard.svelte';
	import Tag from './Tag.svelte';
	import { getDistanceInKm } from '$lib/utils/distance';

	let { data }: { data: PageData } = $props();

	let kind: PlantKind = data.kind;

	// TODO dont hardcode own location
	const ownPosition = {
		label: 'Aalborg, Nordjylland',
		latitude: 57.0369622,
		longitude: 9.9074251
	};
</script>

<div class="flex justify-between w-full pl-10 pt-5">
	<div class="flex">
		<div class="mr-5">
			<img
				alt="Image of a {kind.name}"
				class="h-32 w-32 rounded-full object-cover"
				src={kind.imgUrl}
			/>
		</div>
		<div class="content-center">
			<h1 class="font-sans text-3xl font-bold text-lime-800">{kind.name}</h1>
			<p class="max-w-72 pl-2 text-sm font-bold text-slate-600">
				Lavendel (Lavandula) er en lille slægt med 39 arter i et område. Stedsegrønne eller
				vintergrønne buske eller halvbuske med kompakt vækstform og tæt behårede blade.
			</p>
			<p>
				<Tag label="Bi-venlig" />
				<Tag label="Duft" />
				<Tag label="Staude" />
			</p>
		</div>
	</div>
	<div class="flex flex-col space-y-5">
		<button
			aria-label="Press to want"
			class="justify-center gap-x-2 p-2 rounded-md border-2  border-lime-600 font-semibold text-lime-600 hover:border-lime-500 focus:border-lime-500 focus:outline-none disabled:pointer-events-none disabled:opacity-50"
			type="button">Den mangler jeg!
		</button
		>
		<button
			aria-label="Press if you already have it"
			class="justify-center gap-x-2 p-2 rounded-md border-2 border-lime-600 font-semibold text-lime-600 hover:border-lime-500 focus:border-lime-500 focus:outline-none disabled:pointer-events-none disabled:opacity-50"
			type="button">Den har jeg!
		</button
		>
	</div>
</div>
<div>
	<div>
		<h2 class="pb-5 pt-0 font-sans text-xl font-bold text-slate-600">Hent den her:</h2>
		<div class="grid grid-cols-3 gap-4">
			{#each data.plants as plant}
				<PlantCard
					plant={plant.plant}
					owner={plant.owner}
					distance={getDistanceInKm(ownPosition, plant.owner.position)}
				/>
			{/each}
		</div>
	</div>
</div>
