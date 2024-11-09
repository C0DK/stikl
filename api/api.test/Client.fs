module api.test.APIClient

open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.Logging
open api
open Microsoft.Extensions.DependencyInjection


type FakeApi(configureServices: IServiceCollection -> unit) =
    // It has to bind to a random class in the correct project, to work
    inherit WebApplicationFactory<api.Controllers.UserController>()


    override this.ConfigureWebHost builder =
        builder.ConfigureServices configureServices |> ignore


let setCleanRepository builder = builder

let addLogging (services: IServiceCollection) =
    services.AddLogging(fun builder -> builder.AddConsole().AddFilter(fun level -> level >= LogLevel.Trace) |> ignore)
    |> ignore

    services




let getClientWithDependencies (configureServices: IServiceCollection -> IServiceCollection) =
    let api = new FakeApi(configureServices >> addLogging >> ignore)

    api.CreateClient()

let getClientWithUsers =
    Composition.inMemoryUserRepository
    >> Composition.registerUserRepository
    >> getClientWithDependencies

let plainClient () = getClientWithUsers Composition.users

let builder = fun (services: IServiceCollection) -> services

let withUsers users =
    Composition.registerUserRepository (Composition.inMemoryUserRepository users)

let withPlants plants =
    Composition.registerPlantRepository (Composition.inMemoryPlantRepository plants)

let build = getClientWithDependencies
