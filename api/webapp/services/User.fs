module webapp.services.User

open System.Threading.Tasks
open Auth0.ManagementApi
open Auth0.ManagementApi.Models
open Microsoft.Extensions.DependencyInjection
open domain
open webapp
open webapp.Composition

type UserSummary =
    { username: string
      imgUrl: string
      firstName: string option
      fullName: string option }

// TODO: should this be in the domain? or in a service layer of sorts?
type UserSource =
    { get: Username -> User option Task
      getUserById: string -> User option Task
      getFromPrincipal: unit -> User option Task
      list: unit -> UserSummary list Task
      query: string -> UserSummary list Task }

let mapToSummary (u: Models.User) : UserSummary =
    { username = u.UserName
      firstName = Some u.FirstName // TODO better optional this is an optional
      fullName = Some u.FullName
      imgUrl = u.Picture }

let map (dbo: UserDbo) (u: Models.User) : User =
    { username = dbo.username
      firstName = Some u.FirstName // TODO better optional this is an optional
      fullName = Some u.FullName
      imgUrl = u.Picture
      wants = dbo.wants
      seeds = dbo.seeds
      history = dbo.history }

// TODO: cache all these things!

let getById (client: ManagementApiClient) (userStore: UserStore) (userId: string) : User option Task =
    task {
        let! user = client.Users.GetAsync userId

        // TODO handle not found?
        return!
            userStore.get (Username user.UserName)
            |> Task.map (Option.map (fun dbo -> map dbo user))
    }

let get (client: ManagementApiClient) (userStore: UserStore) (username: Username) : User option Task =
    task {
        let request = GetUsersRequest(Query = $"username={username}")
        let! users = client.Users.GetAllAsync request

        return!
            match (users |> Seq.toList) with
            | [ user ] ->
                userStore.get (Username user.UserName)
                |> Task.map (Option.map (fun dbo -> map dbo user))
            | [] -> Task.FromResult None
            | _ -> failwith $"More than one user matched username='{username}'"
    }

let list (client: ManagementApiClient) () : UserSummary list Task =
    task {
        let request = GetUsersRequest()
        let! users = client.Users.GetAllAsync request

        return users |> Seq.map mapToSummary |> Seq.toList
    }


let query (client: ManagementApiClient) (query: string) : UserSummary list Task =
    task {
        // TODO sanitize query arg
        // TODO: require search to have atleast 3 letters?
        if query.Length < 3 then
            return List.empty
        else
            let request = GetUsersRequest(Query = $"name:*{query}*")
            let! users = client.Users.GetAllAsync request

            return users |> Seq.map mapToSummary |> Seq.toList
    }


// TODO: i hit the rate limit :))) 
let register: IServiceCollection -> IServiceCollection =
        Services.registerScoped(fun s ->
            let client = s.GetRequiredService<ManagementApiClient>()
            let userStore = s.GetRequiredService<UserStore>()
            let principal = s.GetRequiredService<Option<Principal>>()
            // TODO should we differentiate regarding this principal stuff so it isnt scoped?

            { get = get client userStore
              getFromPrincipal = fun () -> principal |> Option.map (fun principal -> getById client userStore principal.auth0Id) |> Task.unpackOption
              list = list client
              query = query client
              getUserById = getById client userStore })
