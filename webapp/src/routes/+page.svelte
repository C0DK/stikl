<script lang="ts">
	import SearchBar from '$lib/components/Searchbar.svelte';
</script>

<svelte:head>
	<title>Stikling.io</title>
	<meta content="Stikling.io - find stiklinger nær dig!" name="description" />
</svelte:head>

<div class="container mx-auto flex flex-grow flex-col items-center justify-center">
	<h1 class="font-sans text-3xl">
		Find <span
		class="inline-block rounded-lg bg-gradient-to-r from-lime-600 to-amber-600 bg-clip-text p-1 px-2 font-bold text-transparent hover:animate-pulse-size"
	>Stiklinger, frø og planter</span
	> nær dig
	</h1>
	<p class="mb-8 max-w-md text-center text-lg md:text-xl">
		Deltag i et fælleskab hvor vi gratis deler frø, planer og stiklinger. At undgå industrielt voksede
		planter er ikke bare billigere for dig - men også for miljøet.
	</p>

	<div>
		<label class="sr-only" for="plant-search">Search for plants</label>
		<SearchBar />
	</div>
</div>
