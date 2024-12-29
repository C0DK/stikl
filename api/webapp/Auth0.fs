module webapp.Auth0

open System.Threading.Tasks
open Auth0.ManagementApi
open Auth0.ManagementApi.Models
open domain

type UserSummary =
    { username: string
      imgUrl: string
      firstName: string option
      fullName: string option }

// TODO: merge with domain!
type User =
    // TODO: get domain variant?
    { username: string
      imgUrl: string
      firstName: string option
      fullName: string option
      wants: Plant list
      seeds: Plant list }


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
let mapAuth0User (getWants: Username -> Plant list) (getHas: Username -> Plant list) (u: Models.User) : User =
    let username = Username u.UserName

    { username = u.UserName
      firstName = Some u.FirstName // TODO better optional this is an optional
      fullName = Some u.FullName
      imgUrl = u.Picture
      wants = getWants username
      seeds = getHas username }

// TODO: cache all these things!
let mapping (user: Models.User) =
    let domUser =
        Composition.users |> List.tryFind (fun u -> u.username.value = user.UserName)

    let getPlant id =
        Composition.plants |> List.find (fun p -> p.id = id)

    mapAuth0User
        (fun u ->
            domUser
            |> Option.map ((_.wants) >> (Seq.map getPlant))
            |> Option.defaultValue Set.empty
            |> Seq.toList)
        (fun u ->
            domUser
            |> Option.map ((_.seeds) >> (Seq.map getPlant))
            |> Option.defaultValue Set.empty
            |> Seq.toList)
        user

let getUserById (client: ManagementApiClient) (userId: string) : User option Task =
    task {
        let! user = client.Users.GetAsync userId

        // TODO handle not found?
        return Some(mapping user)
    }

let getUser (client: ManagementApiClient) (username: Username) : User option Task =
    task {
        let request = GetUsersRequest(Query = $"username={username}")
        let! users = client.Users.GetAllAsync request

        return
            match (users |> Seq.toList) with
            | [ user ] -> Some(mapping user)
            | [] -> None
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
