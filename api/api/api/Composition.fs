module api.Composition

open System
open System.IdentityModel.Tokens.Jwt
open System.Text
open System.Threading.Tasks
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.Extensions.DependencyInjection
open Microsoft.IdentityModel.Tokens
open domain


let basil = Guid.Parse "1265C604-6AD6-4102-8E36-8DA97D25DE8A"


let users =
    [ { id = UserId "cabang"
        wants = Set.empty
        seeds = Set.singleton basil
        history = List.empty }
      { id = UserId "freddy"
        wants = Set.singleton basil
        seeds = Set.empty
        history = List.empty } ]

type UserRepository =
    { getUsers: unit -> User List Task
      getUser: UserId -> User Option Task
      create: UserId -> Result<unit, string> Task
      applyEvent: UserEvent -> UserId -> Result<UserEvent, string> Task }

let inMemoryUserProvider (users: User List) =

    let mutable users = users

    let updateUser func userId =
        users <-
            users
            |> List.map (function
                | user when user.id = userId -> func user
                | user -> user)

    let tryGetUser id =
        users |> List.tryFind (fun user -> user.id = id)

    { getUsers = fun () -> users |> Task.FromResult
      getUser = tryGetUser >> Task.FromResult
      create =
        fun id ->
            match tryGetUser id with
            | Some _ -> Error "User Already Exists!" |> Task.FromResult
            | None ->
                users <- (User.create id) :: users
                Ok() |> Task.FromResult
      applyEvent =
        (fun event userId ->
            (
            // This get might be irrelevant, but it's to ensure that it fails.
            match tryGetUser userId with
            | Some user ->
                updateUser (apply event) user.id

                Ok event |> Task.FromResult
            | None -> Error "User Not Found" |> Task.FromResult)) }



let register (service: 'a) (services: IServiceCollection) =
    services.AddSingleton<'a>(service) |> ignore

    services


let registerUserRepository (provider: UserRepository) =
    register provider.getUser
    >> register provider.getUsers
    >> register provider.applyEvent
    >> register provider.create

let registerAll (services: IServiceCollection) =
    services |> registerUserRepository (inMemoryUserProvider users) |> ignore

module Authentication =

    let secret = "This_is_a_super_secure_key_and_you_know_it"
    // TODO get jwt token variant too, or set these separately in tests
    let signingKey = SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret))


    let validAudiences =
        [ "http://localhost:16097"
          "https://localhost:44330"
          "http://localhost:5213"
          "https://localhost:7053" ]

    let validIssuers = [ "dotnet-user-jwts"; "unittests" ]

    let configureJwtAuth (services: IServiceCollection) =
        services
            .AddAuthentication("Bearer")
            .AddJwtBearer(fun options ->
                options.IncludeErrorDetails <- true

                options.TokenValidationParameters.IssuerSigningKeys <-
                    signingKey
                    :: (options.TokenValidationParameters.IssuerSigningKeys |> Seq.toList)

                options.TokenValidationParameters.ValidAudiences <- validAudiences
                options.TokenValidationParameters.ValidIssuers <- validIssuers)

        |> ignore


        services
