module Stikl.Web.Pages.User.Details

open Stikl.Web
open Stikl.Web.Components
open domain

let heading (user: User) =
    //language=HTML
    $"""
     <div class="flex">
        <img
            alt="Image of a {user.fullName |> String.escape}"
            class="p-2 aspect-square h-32 rounded-full object-cover"
            src="{user.imgUrl}"
        />
        <span class="inline content-center">
            <h1 class="font-sans text-3xl font-bold text-lime-800">{user.fullName |> String.escape}</h1>
            <p class="pl-2 text-sm font-bold text-slate-600">
                {user.location.location.label}
            </p>
        </pan>
     </div>
     """

let render (user: User) (plantCardBuilder: Plant -> string) =
    let locale = Localization.``default``

    let plantArea title (plants: Plant Set) =
        if plants.IsEmpty then
            ""
        else
            let cards = plants |> Seq.map plantCardBuilder
            Common.SectionHeader title + CardGrid.render cards

    let needsPlantArea = plantArea $"{locale.userDetails.wants}:" user.wants
    // TODO show what kind of seeds + the comment
    let seedsPlantArea =
        plantArea $"{locale.userDetails.offers}:" (user.seeds |> Set.map _.plant)


    // language=HTML
    heading user + seedsPlantArea + needsPlantArea

let renderWithStream (user: User) (plantCardBuilder: PlantCard.Builder) =
    sse.streamDivWithInitialValue (render user plantCardBuilder.render) $"/u/{user.username}/sse"
