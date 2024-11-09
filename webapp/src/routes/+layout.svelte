<script lang="ts">
	import '../app.postcss';
	import { AppShell, LightSwitch } from '@skeletonlabs/skeleton';
	import Header from "../components/header.svelte"



	// Floating UI for Popups
	import { computePosition, autoUpdate, flip, shift, offset, arrow } from '@floating-ui/dom';
	import { storePopup } from '@skeletonlabs/skeleton';
	storePopup.set({ computePosition, autoUpdate, flip, shift, offset, arrow });
</script>

<!-- App Shell -->
<AppShell>
	<svelte:fragment slot="header">
		<Header/>
	</svelte:fragment>
	<!-- Page Route Content -->
	<slot />
	<svelte:fragment slot="footer">
		<div class="p-2">

			<LightSwitch />
		</div>
	</svelte:fragment>
</AppShell>
