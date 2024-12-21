module webapp.Router

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.HttpResults

open FSharp.MinimalApi
open FSharp.MinimalApi.Builder
open type TypedResults
open webapp
open domain

let toPlantCards l =
    l |> List.map Components.plantCard |> String.concat "\n"

let routes =
    endpoints {
        get "/" (fun () ->
            let stiklingerFrøOgPlanter =
                Components.themeGradiantSpan "Stiklinger, frø og planter"

            let title = Htmx.PageHeader $"Find {stiklingerFrøOgPlanter} nær dig"

            let callToAction =
                (Htmx.p
                    "mb-8 max-w-md text-center text-lg md:text-xl"
                    "Deltag i et fælleskab hvor vi gratis deler frø, planer og stiklinger. At undgå industrielt voksede planter er ikke bare billigere for dig - men også for miljøet.")


            Htmx.page (title + callToAction + Components.search))

        get "/search" (fun (req : {| query:string |}) ->
            let plantCards =
                Composition.plants
                |> List.filter (_.name.ToLower().Contains(req.query.ToLower()))
                |> toPlantCards

            Htmx.toResult plantCards)

        get "/plant" (fun () ->
            let plantCards = Composition.plants |> toPlantCards

            Htmx.page (Components.grid plantCards))

        get "/plant/{id}" (fun (req: {|id:string|}) ->
             let plantOption = Composition.plants |> List.tryFind (fun p -> p.id.ToString() = req.id)
 
             (match plantOption with
              | Some plant ->
                  Htmx.page
                      $"""
                          
 <div class="flex w-full justify-between pl-10 pt-5">
 	<div class="flex">
 		<div class="mr-5">
 			<img
 				alt="Image of a {plant.name}"
 				class="h-32 w-32 rounded-full object-cover"
 				src={plant.image_url}
 			/>
 		</div>
 		<div class="content-center">
 			<h1 class="font-sans text-3xl font-bold text-lime-800">{plant.name}</h1>
 			<p class="max-w-72 pl-2 text-sm font-bold text-slate-600">
    TODO beskrivelse og tags?
 			</p>
 		</div>
 	</div>
  </div>
 """
              | None ->
                  Htmx.page (
                      (Htmx.PageHeader "Plant not found!")
                      + (Htmx.p "text-center text-lg md:text-xl" $"No plant exists with id {Components.themeGradiantSpan req.id}")
                      + Components.search
                  )))
 
    }
