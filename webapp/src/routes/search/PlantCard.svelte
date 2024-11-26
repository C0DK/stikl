<script lang="ts">
	import type { PlantKind } from '$lib/types';

	let { plant }: { plant: PlantKind } = $props();
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
		<a
			class="cursor-pointer text-sm text-lime-600 underline hover:text-lime-400"
			href="/plant/{plant.id}">Læs mere</a
		>
	</div>
	<h4 class="text-l mb-2 h-auto p-2 italic text-gray-600 dark:text-white">
		{plant.name}
	</h4>
</div>
