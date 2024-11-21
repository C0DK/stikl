<script lang="ts">
    let { small = false }: { small: boolean } = $props();
    let
</script>
<div class="flex rounded-lg shadow-sm {small ? 'w-32' : ''}">
    <input
            type="text"
            id="plant-search"
            name="plant-search"
            placeholder={small ? "Søg..." : "Søg efter planter..."}
            class="block w-full rounded-s-lg border-gray-200 {small ?'px-2 py-1' : 'px-4 py-3'} shadow-sm focus:z-10 focus:border-lime-500 focus:ring-lime-500 disabled:pointer-events-none disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400 dark:placeholder-neutral-500 dark:focus:ring-neutral-600"
    />
    <button
            type="button"
            aria-label="Press to search"
            class="inline-flex {small ? 'w-[2rem]' : 'w-[3rem]'} shrink-0 items-center justify-center gap-x-2 rounded-e-md border border-transparent bg-lime-600 font-semibold text-white hover:bg-lime-500 focus:bg-lime-500 focus:outline-none disabled:pointer-events-none disabled:opacity-50"
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
