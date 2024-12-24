module webapp.routes.User

open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open type TypedResults
open webapp
open webapp.Auth0
open webapp.Page
open domain

let toPlantCards l =
    l |> List.map Components.plantCard |> String.concat "\n"

let routes =
    endpoints {
        group "user"

        get
            "/"
            (fun
                (req:
                    {| renderPage: PageBuilder
                       identityClient: UserSource |}) ->
                task {
                    let! users = req.identityClient.list ()

                    let cards = users |> List.map Components.identityCard

                    return req.renderPage.toPage (Components.grid cards)
                })

        get
            "/{username}"
            (fun
                (req:
                    {| renderPage: PageBuilder
                       identityClient: UserSource
                       username: string |}) ->
                task {
                    // TODO: parse username
                    let! userOption = req.identityClient.get (Username req.username)
                        //Composition.users |> List.tryFind (fun u -> u.username.ToString() = req.id)

                    return req.renderPage.toPage (
                        match userOption with
                        | Some user ->

                            let wantsCards =
                                user.wants
                                |> List.map Components.plantCard
                                |> Components.grid

                            let plants =
                                user.seeds
                                |> Seq.map Components.plantCard
                                |> Seq.toList
                                |> Components.grid

                            $"""
         <div class="flex w-full justify-between pl-10 pt-5">
            <div class="flex">
                <div class="mr-5">
                    <img
                        alt="Image of a {user.username}"
                        class="h-32 w-32 rounded-full object-cover"
                        src={user.username}
                    />
                </div>
                <div class="content-center">
                    <h1 class="font-sans text-3xl font-bold text-lime-800">{user.username}</h1>
                    <p class="max-w-72 pl-2 text-sm font-bold text-slate-600">
                        Location etc
                    </p>
                </div>
                
                <h1>Wants</h1>
                {wantsCards}
                <h1>Has</h1>
                {plants}
            </div>
          </div>
         """
                        | None ->
                            // TODO dedicated 404 helper?
                            ((Components.PageHeader "User not found!")
                             + $"""
        <p class="text-center text-lg md:text-xl">
          No user exists with id {Components.themeGradiantSpan req.username}
        </p>
        """
                             + Components.search)
                    )
                })


    }
