module webapp.services.User

open Microsoft.Extensions.DependencyInjection
open domain
open webapp
open webapp.Composition
open webapp.services.Auth0

let map (dbo: UserDbo) (u: Auth0User) : User =
    { username = dbo.username
      firstName = u.firstName
      fullName = u.fullName
      imgUrl = u.imgUrl
      wants = dbo.wants
      seeds = dbo.seeds
      history = dbo.history }

// TODO: should interface be in the domain? or in a service layer of sorts? maybe merge with the store?
type UserSource(auth0Client: Auth0Client, store: UserStore) as this =
    member _.query = auth0Client.query

    member _.list = auth0Client.list

    member this.get =
        auth0Client.get >> Task.collect (Option.map this.toDom >> Task.unpackOption)

    member this.getFromPrincipal() =
        auth0Client.getOfPrincipal ()
        |> Task.collect (Option.map this.toDom >> Task.unpackOption)

    member private _.toDom(user: Auth0User) =
        store.get user.username
        |> Task.map (fun dbo ->
            match dbo with
            | Some dbo -> Some(map dbo user)
            | None -> None)

let register: IServiceCollection -> IServiceCollection =
    Services.registerScoped (fun s ->
        let client = s.GetRequiredService<Auth0Client>()
        let userStore = s.GetRequiredService<UserStore>()

        UserSource(client, userStore))
