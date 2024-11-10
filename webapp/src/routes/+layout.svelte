<script lang="ts">
	import Header from './Header.svelte';
	import '../app.css';

	let { children } = $props();
</script>

<div class="container mx-auto flex min-h-screen flex-col">
	<Header />

	<main class="container mx-auto flex flex-grow flex-col items-center justify-center space-y-8 p-2">
		{@render children()}
	</main>

	<footer class="bg-lime flex w-full items-center justify-between p-4 text-slate-400">
		<p class="text-sm">© 2024 Stikling.io. All rights reserved.</p>
	</footer>
</div>

<style>
</style>
