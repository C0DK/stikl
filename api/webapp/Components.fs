module webapp.Components

open Microsoft.AspNetCore.Antiforgery
open domain

let search =
    """
<input
   class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:border-lime-500 focus:ring-transparent block p-2.5"
   type="search"
   name="query" placeholder="Søg efter planter eller personer..."
   hx-get="/search"
   hx-trigger="input changed delay:500ms, keyup[key=='Enter']"
   hx-target="#search-results"
   hx-indicator=".htmx-indicator">
   
<span class="font-bold text-2xl pulse htmx-indicator">
    ...
</span>

<div id="search-results" class="grid grid-cols-3 gap-4">
</div>
"""

let authedPlantCard (liked: (bool * AntiforgeryTokenSet) option) (plant: Plant) =
    // TODO: do better!
    let actions =
        match liked with
        | Some(liked, antiForgeryToken) ->
            $"""
            <form
                hx-trigger="submit"
                hx-post="/trigger/{if liked then "removeWant" else "wantPlant"}"
                hx-target="closest #plant"
            >
                <input name="{antiForgeryToken.FormFieldName}" type="hidden" value="{antiForgeryToken.RequestToken}" />
                <input name="plantId" type="hidden" value="{plant.id}" />
                          
                <button
                    class="transform rounded-lg border-2 border-lime-600 px-3 py-1 font-sans text-xs font-bold text-lime-600 transition hover:scale-105"
                    type="submit">{if liked then "Dislike" else "Like"}</button>
            </form>"""
        | None -> ""

    $"""
<div
    id="plant"
    class="h-80 w-64 max-w-sm rounded-lg border border-gray-200 bg-white shadow"
>
    <img
        alt="Image of {plant.name}"
        class="h-3/4 w-64 rounded-t-lg border-b-2 border-gray-800 object-cover"
        src={plant.image_url}
    />
    <div class="float-right mr-2 mt-2 flex flex-col space-y-4">
        <a
            class="cursor-pointer text-sm text-lime-600 underline hover:text-lime-400"
            href="/plant/{plant.id}">Læs mere</a
        >
        {actions}
    </div>
    <h4 class="text-l mb-2 h-auto p-2 italic text-gray-600">
        {plant.name}
    </h4>
</div>
"""

let plantCard (plant: Plant) =
    $"""
<div
    class="h-80 w-64 max-w-sm rounded-lg border border-gray-200 bg-white shadow"
>
    <img
        alt="Image of {plant.name}"
        class="h-3/4 w-64 rounded-t-lg border-b-2 border-gray-800 object-cover"
        src={plant.image_url}
    />
    <div class="float-right mr-2 mt-2 flex flex-col space-y-4">
        <a
            class="cursor-pointer text-sm text-lime-600 underline hover:text-lime-400"
            href="/plant/{plant.id}">Læs mere</a
        >
    </div>
    <h4 class="text-l mb-2 h-auto p-2 italic text-gray-600">
        {plant.name}
    </h4>
</div>
"""

let identityCard (user: services.Auth0.Auth0User) =
    $"""
<div
    class="h-80 w-64 max-w-sm rounded-lg border border-gray-200 bg-white shadow"
>
    <img
        alt="Image of {user.username}"
        class="h-3/4 w-64 rounded-t-lg border-b-2 border-gray-800 object-cover"
        src={user.imgUrl}
    />
    <div class="float-right mr-2 mt-2 flex flex-col space-y-4">
        <a
            class="cursor-pointer text-sm text-lime-600 underline hover:text-lime-400"
            href="/user/{user.username}">Se profil</a
        >
    </div>
    <h4 class="text-l mb-2 h-auto p-2 italic text-gray-600">
        {user.fullName.Value} 
    </h4>
</div>
"""

let userCard (user: User) =
    $"""
<div
    class="h-80 w-64 max-w-sm rounded-lg border border-gray-200 bg-white shadow"
>
    <img
        alt="Image of {user.username}"
        class="h-3/4 w-64 rounded-t-lg border-b-2 border-gray-800 object-cover"
        src={user.username}
    />
    <div class="float-right mr-2 mt-2 flex flex-col space-y-4">
        <a
            class="cursor-pointer text-sm text-lime-600 underline hover:text-lime-400"
            href="/user/{user.username}">Se profil</a
        >
    </div>
    <h4 class="text-l mb-2 h-auto p-2 italic text-gray-600">
        {user.username} TODO NAME ETC
    </h4>
</div>
"""

let grid (content: string list) =
    let innerHtml = content |> String.concat "\n"

    $"""
<div class="grid grid-cols-3 gap-4">
    {innerHtml}
</div>
"""

let themeGradiantSpan innerHtml =
    $"""
<span
class="inline-block rounded-lg bg-gradient-to-r from-lime-600 to-amber-600 bg-clip-text font-bold text-transparent hover:animate-pulse-size"
>
    {innerHtml}
</span>
"""

let PageHeader content =
    $"""
<h1 class="font-sans text-3xl">
{content}
</h1>
"""
