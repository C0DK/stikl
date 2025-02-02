module webapp.services.User

open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open domain
open webapp
open webapp.Composition
open webapp.services.Auth0


type UserOfPrincipal =
    | UserOfPrincipal of (unit -> User option Task)

    member this.get =
        match this with
        | UserOfPrincipal f -> f

let getFromPrincipal (auth0Client: Auth0Client) (store: UserStore) () =
    auth0Client.getOfPrincipal ()
    |> Task.collect (Option.bindTask (_.username >> store.Get))
// TODO: should interface be in the domain? or in a service layer of sorts? maybe merge with the store?

let register: IServiceCollection -> IServiceCollection =
    Services.registerScoped (fun s ->
        let client = s.GetRequiredService<Auth0Client>()
        let store = s.GetRequiredService<UserStore>()

        UserOfPrincipal(getFromPrincipal client store))
