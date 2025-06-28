module webapp.services.User

open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open domain
open webapp

type CurrentUser =
    | CurrentUser of (unit -> User option Task)

    member this.get =
        match this with
        | CurrentUser f -> f

let register: IServiceCollection -> IServiceCollection =
    Services.registerScoped (fun s ->
        let store = s.GetRequiredService<UserStore>()
        let principal = s.GetService<Principal option>()

        CurrentUser(fun () -> principal |> Option.bindTask (_.auth0Id >> store.GetByAuthId)))
