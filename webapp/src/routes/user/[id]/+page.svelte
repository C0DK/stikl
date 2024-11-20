<script lang="ts">
    import type {User} from '$lib/types';
    import type {PageData} from './$types';
    import PlantCard from './plant-card.svelte';

    let {data}: { data: PageData } = $props();
    let user: User = data.user;
</script>
<div class="pl-10 pt-5 w-full flex">
    <div class="mr-5">
        <img src={user.profileImg} class="h-32 w-32  rounded-full object-cover" alt="{user.firstName}'s profile photo"/>
    </div>
    <div class="content-center">
        <h1 class="font-sans text-3xl font-bold text-lime-800">{user.fullName}</h1>
        <h2 class="pt-0 font-sans text-xl italic text-slate-600">
            {user.location.label} <span class="text-sm">({user.location.distanceKm}KM væk)</span>
        </h2>
        <p class="pt-2 text-slate-600 text-sm">Givet 50 | Modtaget 9 | Medlem siden 2023</p>
    </div>
</div>
<div>
    <div>
        <h2 class="pb-5 pt-0 font-sans text-xl font-bold text-slate-600">{user.firstName} har:</h2>
        <div class="grid grid-cols-3 gap-4">
            {#each user.has as plant}
                <PlantCard {plant}/>
            {/each}
        </div>
    </div>
    <div class="mt-10">
        <h2 class="pb-5 pt-0 font-sans text-xl font-bold text-slate-600">{user.firstName} Søger:</h2>
        <div class="grid grid-cols-3 gap-8">
            {#each user.needs as plant}
                <PlantCard {plant}/>
            {/each}
        </div>
    </div>
</div>
