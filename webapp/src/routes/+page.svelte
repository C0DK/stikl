<script lang="ts">
</script>
<div class="container h-full mx-auto flex justify-center items-center">
	<div class="space-y-20 text-center flex flex-col items-center">
		<section>
			<h2 class="h1">Velkommen til Stikling.io</h2>
			<p class="text-2xl text-secondary-900 dark:text-secondary-500">Find, byt og forær stiklinger, frø og meget andet -
				fra plantelskere nær dig </p>
		</section>
		<section class="pulse-bg" />
		<div class="max-w-96">
			<div class="input-group input-group-divider grid-cols-[auto_1fr_auto]">
				<input type="search" placeholder="Søg efter planter..." />
				<button class="variant-filled-primary">Søg</button>
			</div>
		</div>
	</div>
</div>

<style lang="postcss">
    .pulse-bg {
        @apply w-32 h-32 md:w-80 md:h-80;
    }

    .pulse-bg {
        @apply absolute z-[-1] rounded-full blur-[50px] transition-all;
        animation: pulse 5s cubic-bezier(0, 0, 0, 0.1) infinite,
        glow 5s linear infinite;
    }

    @keyframes glow {
        0% {
            @apply bg-primary-400/50;
        }
        33% {
            @apply bg-secondary-400/50;
        }
        66% {
            @apply bg-tertiary-400/50;
        }
        100% {
            @apply bg-primary-400/50;
        }
    }

    @keyframes pulse {
        50% {
            transform: scale(1.5);
        }
    }
</style>
