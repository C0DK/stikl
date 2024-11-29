<script lang="ts">
	import type { Profile } from '$lib/types';

	let { user }: { user: Profile } = $props();
</script>

<div
	class="h-80 w-64 max-w-sm rounded-lg border border-gray-200 bg-white shadow dark:border-gray-700 dark:bg-gray-800"
>
	<img
		alt="Image of {user.firstName}"
		class="h-3/4 w-64 rounded-t-lg border-b-2 border-gray-800 object-cover"
		src={user.profileImg}
	/>
	<div class="mr-2 mt-2 flex flex-col space-y-2 pl-2">
		<a
			class="hover:text-lime-00 h-auto cursor-pointer text-xl font-bold text-lime-600 dark:text-white"
			href="/user/{user.userName}">{user.fullName}</a
		>
		<p>{user.position.label}</p>
	</div>
</div>
