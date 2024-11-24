<script lang="ts">
	import type { User } from '$lib/types';
	import type { PageData } from './$types';
	import PlantCard from './PlantCard.svelte';
	import { getDistanceInKm } from '$lib/utils/distance';

	let { data }: { data: PageData } = $props();
	let user: User = data.user;

	// TODO dont hardcode own location
	let distance = getDistanceInKm(
		{
			label: 'Aalborg, Nordjylland',
			latitude: 57.0369622,
			longitude: 9.9074251
		},
		user.position
	);
</script>

<div class="flex w-full pl-10 pt-5">
	<div class="mr-5">
		<img
			alt="{user.firstName}'s profile photo"
			class="h-32 w-32 rounded-full object-cover"
			src={user.profileImg}
		/>
	</div>
	<div class="content-center">
		<h1 class="font-sans text-3xl font-bold text-lime-800">{user.fullName}</h1>
		<p class="pl-2 text-sm font-bold text-lime-700">99 Følgere</p>
		<h2 class="font-sans text-xl italic text-slate-600">
			{user.position.label}
			<span class="text-sm">({distance.amount.toFixed(0)} {distance.unit})</span>
		</h2>
		<p class="text-sm text-slate-600">Givet 50 | Fået 9 | Medlem siden 2023</p>
	</div>
</div>
<div>
	<div>
		<h2 class="pb-5 pt-0 font-sans text-xl font-bold text-slate-600">{user.firstName} giver:</h2>
		<div class="grid grid-cols-3 gap-4">
			{#each user.has as plant}
				<PlantCard plant={plant.plant} buttonLabel="Ja tak" />
			{/each}
		</div>
	</div>
	<div class="mt-10">
		<h2 class="pb-5 pt-0 font-sans text-xl font-bold text-slate-600">{user.firstName} søger:</h2>
		<div class="grid grid-cols-3 gap-8">
			{#each user.needs as plant}
				<PlantCard {plant} buttonLabel="Giv" />
			{/each}
		</div>
	</div>
</div>
