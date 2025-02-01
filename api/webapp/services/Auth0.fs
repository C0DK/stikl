module webapp.services.Auth0


open System.Threading.Tasks
open Auth0.ManagementApi
open Auth0.ManagementApi.Models
open Microsoft.Extensions.Caching.Memory
open Microsoft.Extensions.DependencyInjection
open domain
open webapp

type Auth0User =
    { username: Username
      imgUrl: string
      firstName: string option
      fullName: string option }

let map (u: Models.User) : Auth0User =
    { username = Username u.UserName
      firstName = u.FirstName |> String.OptionFromNullOrEmpty
      fullName = u.FullName |> String.OptionFromNullOrEmpty
      imgUrl = u.Picture }

type Auth0Client(client: ManagementApiClient, cache: IMemoryCache, getPrincipal: unit -> Principal option) as this =
    // TODO: store all users in memory - dont rely on auth0 for other things than auth.. eventually?
    member this.getByAuth0Id(userId: string) =
        this.Cached $"user_{userId}" (fun () -> client.Users.GetAsync userId |> Task.map map)

    member _.get(username: Username) =
        this.Cached $"user_{username.value}" (fun () ->
            client.Users.GetAllAsync(GetUsersRequest(Query = $"username={username}"))
            |> Task.map (fun users ->
                match (users |> Seq.toList) with
                | [ user ] -> Some(map user)
                | [] -> None
                | _ -> failwith $"More than one user matched username='{username}'"))

    member this.getOfPrincipal() =
        getPrincipal ()
        |> Option.map (_.auth0Id >> this.getByAuth0Id)
        |> Task.unpackOptionTask


    member this.list() =
        this.Cached "Users" (fun () ->
            client.Users.GetAllAsync(GetUsersRequest())
            |> Task.map (Seq.toList >> (List.map map)))


    member _.query(query: string) =
        // TODO: require query in general to be atleast 3 characters 
        if query.Length < 3 then
            List.empty |> Task.FromResult
        else
            client.Users.GetAllAsync(GetUsersRequest(Query = $"name:*{query}*"))
            |> (Task.map (Seq.toList >> List.map map))

    member private _.Cached<'a> (key: string) (factory: unit -> 'a Task) : 'a Task =
        cache.GetOrCreate(key, fun e -> factory ())


let register: IServiceCollection -> IServiceCollection =
    Services.registerScoped (fun s ->
        let client = s.GetRequiredService<ManagementApiClient>()
        let cache = s.GetRequiredService<IMemoryCache>()
        let getPrincipal = s.GetRequiredService<unit -> Option<Principal>>()

        Auth0Client(client, cache, getPrincipal))
