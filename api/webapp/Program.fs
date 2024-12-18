namespace webapp

#nowarn "20"

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.HttpsPolicy
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

open domain

module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddControllers()

        let app = builder.Build()

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()

        let root () = "Hello World"


        app.MapGet(
            "/",
            Func<IResult>(fun () ->
                Htmx.page [ Htmx.h1 "Hello World"; Htmx.p "hvad så min ven?"; Htmx.h2 "mere text?" ]
                |> Htmx.toResult)
        )

        let plantCard (plant: Plant) =
            let href = $"/plant/{plant.id}"
            Htmx.a ({ href = href; label = plant.name })

        app.MapGet(
            "/plant",
            Func<IResult>(fun () -> Htmx.page (Composition.plants |> List.map plantCard) |> Htmx.toResult)
        )

        app.MapGet(
            "/plant/{id}",
            Func<string, IResult>(fun id ->
                let plantOption = Composition.plants |> List.tryFind (fun p -> p.id.ToString() = id)

                (match plantOption with
                 | Some plant -> Htmx.page [ Htmx.h1 $"{plant.name}"; Htmx.p "Det er en ret flot plante!" ]
                 | None -> Htmx.page [ Htmx.h1 "Plant not found!"; Htmx.p $"No plant exists with id '{id}'" ])
                |> Htmx.toResult)
        )



        app.Run()

        exitCode
