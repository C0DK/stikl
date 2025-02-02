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

let imgCard (id: string) (img: {| src: string; alt: string |}) (title: string) (content: string) =
    $"""
<div
    id='{id}'
    class="h-80 w-64 max-w-sm rounded-lg border border-gray-200 bg-white shadow"
>
    <img
        alt="{img.alt}"
        class="h-3/4 w-64 rounded-t-lg border-b-2 border-gray-800 object-cover"
        src='{img.src}'
    />
    <div class="float-right mr-2 mt-2 flex flex-col space-y-4">
    {content}
    </div>
    <h4 class="text-l mb-2 h-auto p-2 italic text-gray-600">
    {title}
    </h4>
</div>
"""


let authedPlantCard (liked: (bool * AntiforgeryTokenSet) option) (plant: Plant) =
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


    imgCard
        "plant"
        {| alt = $"Image of {plant.name}"
           src = plant.image_url |}
        plant.name
        ($"<a class='cursor-pointer text-sm text-lime-600 underline hover:text-lime-400' href='/plant/{plant.id}'>Læs mere</a>"
         + actions)

let plantCard (plant: Plant) =
    imgCard
        "plant"
        {| alt = $"Image of {plant.name}"
           src = plant.image_url |}
        plant.name
        $"<a class='cursor-pointer text-sm text-lime-600 underline hover:text-lime-400' href='/plant/{plant.id}'>Læs mere</a>"

let identityCard (user: services.Auth0.Auth0User) =
    let name = user.fullName |> Option.defaultValue user.username.value

    imgCard
        "user"
        {| alt = $"Image of {name}"
           src = user.imgUrl |}
        name
        $"<a class='cursor-pointer text-sm text-lime-600 underline hover:text-lime-400' href='/user/{user.username.value}'>Se profil</a>"

let userCard (user: domain.User) =
    let name = user.fullName |> Option.defaultValue user.username.value

    imgCard
        "user"
        {| alt = $"Image of {name}"
           src = user.imgUrl |}
        name
        $"<a class='cursor-pointer text-sm text-lime-600 underline hover:text-lime-400' href='/user/{user.username.value}'>Se profil</a>"

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
