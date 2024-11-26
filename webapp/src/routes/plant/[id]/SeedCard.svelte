<script lang="ts">
	import type { Distance, Plant, User } from '$lib/types';

	let { owner, plant, distance }: { plant: Plant; owner: User; distance?: Distance } = $props();
</script>

<div
	class="w-64 max-w-sm rounded-lg border border-gray-200 bg-white shadow dark:border-gray-700 dark:bg-gray-800"
>
	<button
		aria-label="Press to search"
		class="float-right mr-2 mt-2 w-[4rem] justify-center gap-x-2 rounded-md border border-transparent bg-lime-600 font-semibold text-white hover:bg-lime-500 focus:bg-lime-500 focus:outline-none disabled:pointer-events-none disabled:opacity-50"
		type="button"
		>Ja tak
	</button>
	<div class="h-auto p-2">
		<h5 class="text-l mb-2 font-bold capitalize dark:text-white">{plant.kind}</h5>
		<p>
			Hos
			<a
				class="cursor-pointer text-lime-600 underline hover:text-lime-400"
				href="/user/{owner.userName}">{owner.fullName}</a
			>
			{#if distance}
				<span class="text-slate-600">
					({distance.amount.toFixed(0)}
					{distance.unit})
				</span>
			{/if}
		</p>
		{#if plant.comment}
			<h5 class="text-l mb-2 italic text-gray-600 dark:text-white">{plant.comment}</h5>
		{/if}
	</div>
</div>
