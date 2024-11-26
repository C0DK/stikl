<script lang="ts">
	import SearchBar from '$lib/components/Searchbar.svelte';
	import auth from '$lib/services/auth.svelte';
</script>

<header class="bg-lime-30 flex justify-between p-2">
	<a
		class="rounded-lg bg-gradient-to-br from-lime-600 to-amber-600 px-3 py-1 text-left font-sans text-xl font-semibold text-white hover:underline"
		href="/">Stikling.io</a
	>
	<div class="flex justify-between gap-5">
		<SearchBar small={true} />
		{#if auth.currentUser}
			<div class="flex items-center">
				<span class="text-lime-600">
					Hi, <a
						href="/user/{auth.currentUser.userName}"
						class="cursor-pointer font-semibold underline hover:text-lime-500"
						>{auth.currentUser.firstName}</a
					>
				</span>
			</div>
		{:else if auth.isInitialized}
			<button
				class="transform rounded-lg border-2 border-lime-600 px-3 py-1 font-sans text-sm font-bold text-lime-600 transition hover:scale-105 dark:bg-lime-400 dark:text-lime-400"
				onclick={auth.loginWithPopup()}
			>
				Log ind
			</button>
		{:else}
			Loading...
		{/if}
	</div>
</header>
