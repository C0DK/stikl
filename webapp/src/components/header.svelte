<script>
	import { AppBar } from '@skeletonlabs/skeleton';
</script>
<AppBar background="bg-surface-100-800-token">
	<svelte:fragment slot="lead">
		<a class="text-xl"
			 href="/">
			Stikling.io
		</a>
	</svelte:fragment>
	<svelte:fragment slot="trail">
		<a
			class="btn btn-sm font-bold bg-primary-400 rounded"
			href="https://google.com"
			target="_blank"
			rel="noreferrer"
		>
			Log ind
		</a>
	</svelte:fragment>
</AppBar>
