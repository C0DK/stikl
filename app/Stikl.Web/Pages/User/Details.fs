module Stikl.Web.Pages.User.Details

open Stikl.Web.Components
open domain
open Stikl.Web.Components.Htmx

let heading (user: User) =
    let name = user.lastName |> Option.defaultValue user.username.value

    //language=HTML
    $"""
     <div class="flex">
        <img
            alt="Image of a {name}"
            class="p-2 aspect-square h-32 rounded-full object-cover"
            src="{user.imgUrl}"
        />
        <span class="inline content-center">
            <h1 class="font-sans text-3xl font-bold text-lime-800">{name}</h1>
            <p class="pl-2 text-sm font-bold text-slate-600">
                Location etc
            </p>
        </pan>
     </div>
     """


let render (user: User) (pageBuilder: PageBuilder) =
    let plantArea title plants =
        let cards = plants |> Seq.map pageBuilder.plantCard

        Common.SectionHeader title + CardGrid.render cards

    let needsPlantArea = plantArea "Søger:" user.wants
    // TODO show what kind of seeds + the comment
    let seedsPlantArea = plantArea "Har:" (user.seeds |> Seq.map _.plant)


    // language=HTML
    heading user + seedsPlantArea + needsPlantArea
