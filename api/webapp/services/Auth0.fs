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

    member this.getOfPrincipal() =
        getPrincipal ()
        |> Option.map (_.auth0Id >> this.getByAuth0Id)
        |> Task.unpackOptionTask

    member private _.Cached<'a> (key: string) (factory: unit -> 'a Task) : 'a Task =
        cache.GetOrCreate(key, fun e -> factory ())


let register: IServiceCollection -> IServiceCollection =
    Services.registerScoped (fun s ->
        let client = s.GetRequiredService<ManagementApiClient>()
        let cache = s.GetRequiredService<IMemoryCache>()
        let getPrincipal = s.GetRequiredService<unit -> Option<Principal>>()

        Auth0Client(client, cache, getPrincipal))
