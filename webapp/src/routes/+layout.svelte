<script lang="ts">
	import Header from './Header.svelte';
	import {AppShell} from "@skeletonlabs/skeleton"
	import '../app.css';

	let { children } = $props();

	let isDarkMode = $state(true);

	function toggleDarkMode() {
		isDarkMode = !isDarkMode;
	}

</script>

<div
	data-theme="vintage"
	class="">
	<Header />

	<main class="container mx-auto p-8 space-y-8">
		{@render children()}
	</main>

	<footer
		class="w-full bg-secondary dark:bg-dark-secondary text-neutral-dark dark:text-neutral-light p-4 flex items-center justify-between">
		<p class="text-sm">© 2024 Stikling.io. All rights reserved.</p>

		<!-- Dark Mode Toggle Button with Icon -->
		<button onclick={toggleDarkMode} class="p-2 rounded-full bg-neutral-light dark:bg-neutral-dark">
			{#if isDarkMode}
				<!-- Sun Icon for Light Mode -->
				<svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 text-yellow-400" viewBox="0 0 24 24" fill="currentColor">
					<path
						d="M12 2a1 1 0 010 2V2zM6.635 4.343a1 1 0 01-.707-.293l-1.414 1.414A1 1 0 014.343 4.05l1.414-1.414a1 1 0 01.707.293zM4.05 19.657a1 1 0 01.293-.707L2.93 18.536a1 1 0 010 1.414l1.414 1.414a1 1 0 01-.293-.707zM12 20a1 1 0 010-2v2zm7.071-15.071a1 1 0 01-.293.707l1.414 1.414a1 1 0 010-1.414l-1.414-1.414a1 1 0 01.293.707zM12 4a8 8 0 108 8 8.009 8.009 0 00-8-8zm0 14a6 6 0 110-12 6 6 0 010 12z" />
				</svg>
			{:else}
				<!-- Moon Icon for Dark Mode -->
				<svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 text-yellow-200" viewBox="0 0 24 24" fill="currentColor">
					<path d="M20.742 13.045a8 8 0 11-9.763-9.763 10 10 0 109.763 9.763z" />
				</svg>
			{/if}
		</button>
	</footer>
</div>

<style>
</style>
