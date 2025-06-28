module webapp.Data.Inmemory

open System.Threading.Tasks
open domain

type UserDbo =
    // TODO Eventually we might need to add the auth0 id, so we can correlate the users. - also to swap auth provider.
    { username: Username
      authId: string option
      firstName: string option
      lastName: string option
      imgUrl: string option
      wants: Plant Set
      seeds: PlantOffer Set
      history: UserEventPayload List }

module UserDbo =
    let create (user: User) : UserDbo =
        { username = user.username
          authId = user.authId
          firstName = user.firstName
          // TODO: this is bad
          lastName = user.fullName |> Option.map (fun n -> n.Split [| ' ' |] |> Seq.last)
          imgUrl = Some user.imgUrl
          wants = user.wants
          seeds = user.seeds
          history = user.history }

let toDom (user: UserDbo) : User =
    { username = user.username
      authId = user.authId
      imgUrl =
        user.imgUrl
        |> Option.defaultValue $"https://api.dicebear.com/9.x/shapes/png?seed={user.username.value}"
      firstName = user.firstName
      fullName = Option.map2 (fun firstName lastName -> $"{firstName} {lastName}") user.firstName user.lastName
      wants = user.wants
      seeds = user.seeds
      history = user.history }

type InMemoryUserRepository(users: User seq) =
    let mutable users = users |> Seq.toList |> List.map UserDbo.create

    // TODO: if events are the only thing to change a user, we could probably join these +
    //       include the validation that user exists
    let updateUser func username =
        users <-
            users
            |> List.map (function
                | user when user.username = username -> (toDom user) |> func |> UserDbo.create
                | user -> user)

    let tryGetUser username =
        users |> List.tryFind (fun user -> user.username = username)

    let tryGetUserByAuthId authId =
        users |> List.tryFind (fun user -> user.authId = Some authId)

    interface UserStore with
        member this.Get(username: Username) : User option Task =
            tryGetUser username |> Option.map toDom |> Task.FromResult

        member this.GetByAuthId(authId: string) : User option Task =
            tryGetUserByAuthId authId |> Option.map toDom |> Task.FromResult

        member this.GetAll() : User list Task =
            users |> List.map toDom |> Task.FromResult

        member this.Query(query: string) : User list Task =
            let isMatch (v: string) = v.Contains query

            users
            |> List.filter (fun user ->
                isMatch user.username.value
                || user.firstName |> Option.map isMatch |> Option.defaultValue false
                || user.lastName |> Option.map isMatch |> Option.defaultValue false)
            |> List.map toDom
            |> Task.FromResult


        member this.ApplyEvent (event: UserEvent) : Result<UserEvent, string> Task =
            (match tryGetUser event.user with
             | Some user ->
                 do updateUser (apply event.payload) user.username
                 Ok event |> Task.FromResult
             | None -> Error $"User '{event.user}' Not Found" |> Task.FromResult)
