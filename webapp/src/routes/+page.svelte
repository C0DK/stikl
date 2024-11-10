<script lang="ts">
</script>

<svelte:head>
	<title>Stikling.io</title>
	<meta name="description" content="Stikling.io - find stiklinger nær dig!" />
</svelte:head>

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
	<label for="plant-search" class="sr-only">Search for plants</label>
	<div class="flex rounded-lg shadow-sm">
		<input
			type="text"
			id="plant-search"
			name="plant-search"
			placeholder="Søg efter planter..."
			class="block w-full rounded-s-lg border-gray-200 px-4 py-3 shadow-sm focus:z-10 focus:border-lime-500 focus:ring-lime-500 disabled:pointer-events-none disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400 dark:placeholder-neutral-500 dark:focus:ring-neutral-600"
		/>
		<button
			type="button"
			aria-label="Press to search"
			class="inline-flex w-[3rem] shrink-0 items-center justify-center gap-x-2 rounded-e-md border border-transparent bg-lime-600 font-semibold text-white hover:bg-lime-500 focus:bg-lime-500 focus:outline-none disabled:pointer-events-none disabled:opacity-50"
		>
			<svg
				class="size-4 shrink-0"
				xmlns="http://www.w3.org/2000/svg"
				width="24"
				height="24"
				viewBox="0 0 24 24"
				fill="none"
				stroke="currentColor"
				stroke-width="2"
				stroke-linecap="round"
				stroke-linejoin="round"
			>
				<circle cx="11" cy="11" r="8"></circle>
				<path d="m21 21-4.3-4.3"></path>
			</svg>
		</button>
	</div>
</div>
