module webapp.Auth0

open System.Threading.Tasks
open Auth0.ManagementApi
open Auth0.ManagementApi.Models
open domain

// TODO: probably call the "principal identity" something different?
type Identity =
    { username: string
      imgUrl: string
      firstName: string option
      fullName: string option }

type IdentityClient =
    {
      // TODO: use username instead of id?
      getUser: UserId -> Identity option Task
      listUsers: unit -> Identity list Task }

let mapAuth0User (u: Models.User) : Identity =
    { username = u.UserName
      firstName = Some u.FirstName // TODO better optional this is an optional
      fullName = Some u.FullName
      imgUrl = u.Picture }

let getIdentity (client: ManagementApiClient) (id: UserId) : Identity option Task =
    // TODO: this is probably the wrong id..? this is the auth0|.. id - we want to query by username prolly
    // TODO will probably fail?
    try
        client.Users.GetAsync id.value |> Task.map (mapAuth0User >> Some)
    with :? Auth0.Core.Exceptions.ErrorApiException as exc when exc.Message = "Not Found" ->
        None |> Task.FromResult

let listUsers (client: ManagementApiClient) () : Identity list Task =
    task {
        let request = GetUsersRequest()
        let! users = client.Users.GetAllAsync request

        return users |> Seq.map mapAuth0User |> Seq.toList
    }
