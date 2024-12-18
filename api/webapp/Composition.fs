module webapp.Composition

open System
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open domain


let basil =
    { id = Guid.Parse "1265C604-6AD6-4102-8E36-8DA97D25DE8A"
      name = "Basil"
      image_url = "https://bs.plantnet.org/image/o/3762433643a1e7307243999389cb652b85737c57" }

let rosemary =
    { id = Guid.Parse "F691103B-6209-45A1-B1A2-2F53CC48C99C"
      name = "Rosemary"
      image_url = "https://bs.plantnet.org/image/o/b26911a43364183594d57a147d63572cdf8c938b" }

let lavender =
    { id = Guid.Parse "A68A04F2-DF42-422F-9EBE-EF6132B61619"
      name = "Topped Lavender"
      image_url = "https://bs.plantnet.org/image/o/034910084fec5cbd4c2635d6549212636fb8fdf2" }

let plants =
    [ basil
      rosemary
      lavender

      ]

let users =
    [ { id = UserId "cabang"
        wants = Set.empty
        seeds = Set.singleton basil.id
        history = List.empty }
      { id = UserId "freddy"
        wants = Set.singleton basil.id
        seeds = Set.singleton lavender.id
        history = List.empty } ]

type PlantRepository =
    { getAll: unit -> Plant List Task
      get: PlantId -> Plant Option Task
      exists: PlantId -> bool Task }

type UserRepository =
    { getAll: unit -> User List Task
      get: UserId -> User Option Task
      create: UserId -> Result<unit, string> Task
      applyEvent: UserEvent -> UserId -> Result<UserEvent, string> Task }

let inMemoryPlantRepository (entities: Plant List) =
    let mutable entities = entities

    let tryGet id =
        entities |> List.tryFind (fun entity -> entity.id = id)

    { getAll = fun () -> entities |> Task.FromResult
      get = tryGet >> Task.FromResult
      exists = tryGet >> Option.isSome >> Task.FromResult }

let inMemoryUserRepository (users: User List) =

    let mutable users = users

    let updateUser func userId =
        users <-
            users
            |> List.map (function
                | user when user.id = userId -> func user
                | user -> user)

    let tryGetUser id =
        users |> List.tryFind (fun user -> user.id = id)

    { getAll = fun () -> users |> Task.FromResult
      get = tryGetUser >> Task.FromResult
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


let registerUserRepository (repository: UserRepository) =
    register repository.get
    >> register repository.getAll
    >> register repository.applyEvent
    >> register repository.create

let registerPlantRepository (repository: PlantRepository) =
    register repository.get
    >> register repository.getAll
    >> register repository.exists

let registerAll (services: IServiceCollection) =
    services
    |> registerUserRepository (inMemoryUserRepository users)
    |> registerPlantRepository (inMemoryPlantRepository plants)
    |> ignore
