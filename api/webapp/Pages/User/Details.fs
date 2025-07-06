module webapp.Pages.User.Details

open domain
open webapp
open webapp.Components.Htmx

let render (user: User) (pageBuilder: PageBuilder) =
    task {
        let plantArea title plants =
            task {
                let cardGrid =
                    plants
                    |> Seq.map pageBuilder.plantCard
                    |> Components.Common.grid

                return
                    $"""                           
                                         <div class="flex flex-col justify-items-center">
                                            {Components.Common.PageHeader title}
                                            {cardGrid}
                                          </div>"""
            }

        let name = user.fullName |> Option.defaultValue user.username.value

        let! needsPlantArea = plantArea $"{name} søger:" user.wants
        // TODO handle plant
        let! seedsPlantArea = plantArea $"{name} har:" (user.seeds |> Seq.map _.plant)

        let events =
            user.history
            |> Seq.map (fun e -> $"<li>{e.ToString()}</li>")
            |> String.concat "\n"


        let events = $"<ul>{events}</ul>"

        return
            $"""
             <div class="flex w-full justify-between pl-10 pt-5">
                <div class="flex">
                    <div class="mr-5">
                        <img
                            alt="Image of a {name}"
                            class="h-32 w-32 rounded-full object-cover"
                            src="{user.imgUrl}"
                        />
                    </div>
                    <div class="content-center">
                        <h1 class="font-sans text-3xl font-bold text-lime-800">{name}</h1>
                        <p class="max-w-72 pl-2 text-sm font-bold text-slate-600">
                            Location etc
                        </p>
                    </div>
                    
                </div>
            </div>
            {seedsPlantArea}
            {needsPlantArea}
            <div class="flex flex-col justify-items-center">
               {Components.Common.PageHeader "History"}
               {events}
            </div>
            """
    }
