module api.test.APIClient

open Microsoft.AspNetCore.Mvc.Testing
open api
open Microsoft.Extensions.DependencyInjection


type FakeApi(configureServices: IServiceCollection -> unit) =
    // It has to bind to a random class in the correct project, to work
    inherit WebApplicationFactory<api.Controllers.UserController>()


    override this.ConfigureWebHost builder =
        builder.ConfigureServices configureServices |> ignore


let setCleanRepository builder = builder

let getClientWithDependencies (configureServices: IServiceCollection -> IServiceCollection) =
    let api = new FakeApi(configureServices >> ignore)
    api.CreateClient()

let getClientWithUsers =
    Composition.inMemoryUserProvider >> Composition.registerUserRepository >> getClientWithDependencies

let getClient () =
    getClientWithUsers Composition.users

