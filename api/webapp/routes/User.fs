module webapp.routes.User

open Microsoft.AspNetCore.Http

open FSharp.MinimalApi.Builder
open type TypedResults
open webapp
open webapp.Page
open domain

let toPlantCards l =
    l |> List.map Components.plantCard |> String.concat "\n"

let routes =
    endpoints {
        group "user"

        get "/" (fun (req: {| pageBuilder: PageBuilder |}) ->
            // TODO user source instead.
            let cards = Composition.users |> List.map Components.userCard

            req.pageBuilder.ToPage(Components.grid cards))

        get
            "/{id}"
            (fun
                (req:
                    {| pageBuilder: PageBuilder
                       id: string |}) ->
                let userOption =
                    Composition.users |> List.tryFind (fun u -> u.id.ToString() = req.id)

                req.pageBuilder.ToPage(
                    match userOption with
                    | Some user ->
                        // TODO use DI
                        let getPlant id =
                            Composition.plants |> List.find (fun p -> p.id = id)
                        let wantsCards = user.wants |> Seq.map (getPlant >> Components.plantCard) |> Seq.toList |> Components.grid
                        let plants = user.seeds |> Seq.map (getPlant >> Components.plantCard) |> Seq.toList |> Components.grid
                        
                        $"""
     <div class="flex w-full justify-between pl-10 pt-5">
        <div class="flex">
            <div class="mr-5">
                <img
                    alt="Image of a {user.id}"
                    class="h-32 w-32 rounded-full object-cover"
                    src={user.id}
                />
            </div>
            <div class="content-center">
                <h1 class="font-sans text-3xl font-bold text-lime-800">{user.id}</h1>
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
      No user exists with id {Components.themeGradiantSpan req.id}
    </p>
    """
                         + Components.search)
                ))


    }
