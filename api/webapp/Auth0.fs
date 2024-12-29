module webapp.Auth0

open System.Threading.Tasks
open Auth0.ManagementApi
open Auth0.ManagementApi.Models
open domain
open webapp.Composition

type UserSummary =
    { username: string
      imgUrl: string
      firstName: string option
      fullName: string option }

type UserSource =
    { get: Username -> User option Task
      getUserById: string -> User option Task
      list: unit -> UserSummary list Task
      query: string -> UserSummary list Task }
    
let mapAuth0UserToSummary (u: Models.User) : UserSummary =
    { username = u.UserName
      firstName = Some u.FirstName // TODO better optional this is an optional
      fullName = Some u.FullName
      imgUrl = u.Picture }

// TODO Task variant.
let mapAuth0User (dbo: UserDbo) (u: Models.User) : User =
    { username = dbo.username
      firstName = Some u.FirstName // TODO better optional this is an optional
      fullName = Some u.FullName
      imgUrl = u.Picture
      wants = dbo.wants
      seeds = dbo.seeds
      history = dbo.history }

// TODO: cache all these things!

let getUserById (client: ManagementApiClient) (userStore: UserStore) (userId: string) : User option Task =
    task {
        let! user = client.Users.GetAsync userId

        // TODO handle not found?
        return! userStore.get (Username user.UserName) |> Task.map (Option.map (fun dbo -> mapAuth0User dbo user))
    }

let getUser (client: ManagementApiClient) (userStore: UserStore) (username: Username) : User option Task =
    task {
        let request = GetUsersRequest(Query = $"username={username}")
        let! users = client.Users.GetAllAsync request

        return!
            match (users |> Seq.toList) with
            | [ user ] -> userStore.get (Username user.UserName) |> Task.map (Option.map (fun dbo -> mapAuth0User dbo user))
            | [] -> Task.FromResult None
            | _ -> failwith $"More than one user matched username='{username}'"
    }

let listUsers (client: ManagementApiClient) () : UserSummary list Task =
    task {
        let request = GetUsersRequest()
        let! users = client.Users.GetAllAsync request

        return users |> Seq.map mapAuth0UserToSummary |> Seq.toList
    }


let queryUsers (client: ManagementApiClient) (query: string) : UserSummary list Task =
    task {
        // TODO sanitize query arg
        // TODO: require search to have atleast 3 letters?
        if query.Length < 3 then
            return List.empty
        else
            let request = GetUsersRequest(Query = $"name:*{query}*")
            let! users = client.Users.GetAllAsync request

            return users |> Seq.map mapAuth0UserToSummary |> Seq.toList
    }
