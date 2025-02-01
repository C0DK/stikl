module webapp.routes.User

open Microsoft.AspNetCore.Http

open System.Threading.Tasks
open FSharp.MinimalApi.Builder
open type TypedResults
open webapp
open webapp.services
open domain

let routes =
    endpoints {
        group "user"

        get
            "/"
            (fun
                (req:
                    {| renderPage: Htmx.PageBuilder
                       users: User.UserSource |}) ->
                task {
                    let! users = req.users.list ()

                    let cards = users |> List.map Components.identityCard

                    return req.renderPage.toPage (Components.grid cards)
                })

        // If buttons are pressed on your OWN page, it is not refreshed with new users.
        get
            "/{username}"
            (fun
                (req:
                    {| pageBuilder: Htmx.PageBuilder
                       users: User.UserSource
                       username: string |}) ->
                task {
                    // TODO: parse/verify username
                    let! userOption = req.users.get (Username req.username)

                    // TODO: use result instead, and generalize 404 pages.
                    let! content =
                        match userOption with
                        | Some user ->
                            task {
                                let plantArea title plants =
                                    task {
                                        let! cardGrid =
                                            plants
                                            |> Seq.map req.pageBuilder.plantCard
                                            |> Task.combine
                                            |> Task.map (Components.grid)

                                        return
                                            $"""                           
                                         <div class="flex flex-col justify-items-center">
                                            {Components.PageHeader title}
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
               {Components.PageHeader "History"}
               {events}
            </div>
            """
                            }
                        | None ->
                            // TODO dedicated 404 helper?
                            Task.FromResult(
                                (Components.PageHeader "User not found!")
                                + $"""
        <p class="text-center text-lg md:text-xl">
          Vi kunne desværre ikke finde {Components.themeGradiantSpan req.username}
        </p>
        """
                                + Components.search
                            )

                    return req.pageBuilder.toPage content
                })


    }
