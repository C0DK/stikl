<script lang="ts">
	import type { User } from '$lib/types';
	import type { PageData } from './$types';
	import PlantCard from './PlantCard.svelte';
	import auth from '$lib/services/auth.svelte';

	let { data }: { data: PageData } = $props();
	let user: User = data.user;

	let distance = auth.distanceTo(user.position);
	const isMe = $derived(auth.currentUser?.userName == user.userName);
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
		<h1 class="font-sans text-3xl font-bold text-lime-800">
			{user.fullName}
			{#if isMe}
				<span class="text-sm text-lime-600">(Dig)</span>
			{/if}
		</h1>
		<p class="pl-2 text-sm font-bold text-lime-700">99 Følgere</p>
		<h2 class="font-sans text-xl italic text-slate-600">
			{user.position.label}
			{#if distance && !isMe}
				<span class="text-sm">({distance.amount.toFixed(0)} {distance.unit})</span>
			{/if}
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
