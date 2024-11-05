namespace api

#nowarn "20"

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting


module Program =
    let exitCode = 0


    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddControllers()

        builder.Services |> Composition.registerAll

        builder.Services.AddSwaggerGen() |> ignore

        let app = builder.Build()

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()

        app.UseSwagger()
        app.UseSwaggerUI()

        app.Run()

        exitCode
