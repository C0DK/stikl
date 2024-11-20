<script lang="ts">
	import type { Plant } from '$lib/types';

	let { plant }: { plant: Plant } = $props();
</script>

<div
	class="h-80 w-64 max-w-sm rounded-lg border border-gray-200 bg-white shadow dark:border-gray-700 dark:bg-gray-800"
>
	<img
		class="h-3/4 w-64 rounded-t-lg border-b-2 border-gray-800 object-cover"
		src={plant.imgUrl}
		alt="Image of {plant.name}"
	/>
	<button
		type="button"
		aria-label="Press to search"
		class="float-right mr-2 mt-2 w-[3rem] justify-center gap-x-2 rounded-md border border-transparent bg-lime-600 font-semibold text-white hover:bg-lime-500 focus:bg-lime-500 focus:outline-none disabled:pointer-events-none disabled:opacity-50"
		>Byt</button
	>
	<div class="h-auto p-2">
		<h5 class="text-l mb-2 italic text-gray-600 dark:text-white">{plant.name}</h5>
	</div>
</div>
