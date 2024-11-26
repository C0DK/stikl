<script lang="ts">
	import type { PlantKind } from '$lib/types';

	let { plant, buttonLabel }: { plant: PlantKind; buttonLabel: string } = $props();
</script>

<div
	class="h-80 w-64 max-w-sm rounded-lg border border-gray-200 bg-white shadow dark:border-gray-700 dark:bg-gray-800"
>
	<img
		alt="Image of {plant.name}"
		class="h-3/4 w-64 rounded-t-lg border-b-2 border-gray-800 object-cover"
		src={plant.imgUrl}
	/>
	<div class="float-right mr-2 mt-2 flex flex-col space-y-4">
		<button
			aria-label="Press to search"
			class="w-[4rem] justify-center gap-x-2 rounded-md border border-transparent bg-lime-600 font-semibold text-white hover:bg-lime-500 focus:bg-lime-500 focus:outline-none disabled:pointer-events-none disabled:opacity-50"
			type="button">{buttonLabel}</button
		>
		<a
			class="hover:text-lime-00 cursor-pointer text-sm text-lime-600 underline"
			href="/plant/{plant.id}">Læs mere</a
		>
	</div>
	<h4 class="text-l mb-2 h-auto p-2 italic text-gray-600 dark:text-white">
		{plant.name}
	</h4>
</div>
