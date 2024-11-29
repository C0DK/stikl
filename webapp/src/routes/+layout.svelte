<script lang="ts">
	import Header from './Header.svelte';
	import '../app.css';
	import { onMount, type Snippet } from 'svelte';
	import auth from '$lib/services/auth.svelte';

	import { PUBLIC_AUTH0_CLIENT_ID, PUBLIC_AUTH0_DOMAIN } from '$env/static/public';
	onMount(async () => {
		await auth.initClient(PUBLIC_AUTH0_DOMAIN, PUBLIC_AUTH0_CLIENT_ID);
		await auth.updateUser();

		console.log(await auth.fetchUser());
	});

	let { children }: { children: Snippet } = $props();
</script>

<div class="container mx-auto flex min-h-screen flex-col">
	<Header />

	<main class="container mx-auto mt-10 flex flex-grow flex-col items-center space-y-8 p-2">
		{@render children()}
	</main>

	<footer class="bg-lime flex w-full items-center justify-between p-4 text-slate-400">
		<p class="text-sm">© 2024 Stikling.io. All rights reserved.</p>
	</footer>
</div>

<style>
</style>
