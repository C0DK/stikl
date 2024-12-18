module webapp.Composition

open System
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open domain


// TODO: dont use guid for id :3
let basil =
    { id = Guid.Parse "1265C604-6AD6-4102-8E36-8DA97D25DE8A"
      name = "Basilikum"
      image_url = "https://bs.plantnet.org/image/o/3762433643a1e7307243999389cb652b85737c57" }

let lavender =
    { id = Guid.Parse "A68A04F2-DF42-422F-9EBE-EF6132B61619"
      name = "Topped Lavender"
      image_url = "https://bs.plantnet.org/image/o/034910084fec5cbd4c2635d6549212636fb8fdf2" }

let sommerfugleBusk =
    { id = Guid.Parse "A6D2B3C8-5B12-4F2E-8D61-EB1A7AC091C4"
      name = "Sommerfuglebusk, Buddleia dav. 'Black Knight'"
      image_url =
        "https://media.plantorama.dk/cdn/2SP93k/sommerfuglebusk-buddleia-dav-black-knight-5-liter-potte-sommerfuglebusk-buddleia-dav-black-knight-5-liter-potte.webp?d=14378" }

let winterSquash =
    { id = Guid.Parse "CF36E97A-2347-4A5D-9A9C-06C6F8C34711"
      name = "Vinter squash"
      image_url = "https://www.gardenia.net/wp-content/uploads/2023/05/cucurbita-maxima-winter-squash-780x520.webp" }

let pepperMint =
    { id = Guid.Parse "D8A4A165-81E4-4D5A-AE65-9C7A837A29F5"
      name = "Peppermynte"
      image_url = "https://www.gardenia.net/wp-content/uploads/2023/05/mentha-piperita-peppermint-780x520.webp" }

let rosemary =
    { id = Guid.Parse "F32A472C-8C11-4321-83E5-743A748A96F1"
      name = "Rosemarin"
      image_url = "https://www.gardenia.net/wp-content/uploads/2023/05/rosmarinus-officinalis-arp-780x520.webp" }

let thyme =
    { id = Guid.Parse "5A7CB8F7-41C3-456A-B7D4-66C9959A2BC6"
      name = "Timian"
      image_url = "https://www.gardenia.net/wp-content/uploads/2023/05/thymus-serpyllum-creeping-thyme-780x520.webp" }

let onion =
    { id = Guid.Parse "4E39449F-0209-485B-A02B-F0E6B2863D38"
      name = "Løg"
      image_url = "https://www.gardenia.net/wp-content/uploads/2023/05/allium-cepa-780x520.webp" }

let sunflowerVelvetQueen =
    { id = Guid.Parse "9049905A-D439-4C55-9B81-B2BE1DEB962D"
      name = "Solsikke (Velvet Queen)"
      image_url = "https://www.gardenia.net/wp-content/uploads/2024/01/shutterstock_2049263999-800x533.jpg" }

let sunflower =
    { id = Guid.Parse "F4DEE605-64F4-4A4F-9C6D-043FF2702F45"
      name = "Solsikke"
      image_url = "https://www.gardenia.net/wp-content/uploads/2023/05/Helianthus-annuus-504x533.webp" }

let dvaergTidsel =
    { id = Guid.Parse "FE627D90-A5E8-46AB-B362-99D3C4939E7F"
      name = "Dværg Tidsel"
      image_url = "https://www.gardenia.net/wp-content/uploads/2023/05/cirsium-acaule-780x520.webp" }

let plants =
    [ basil
      rosemary
      lavender
      dvaergTidsel
      thyme
      pepperMint
      winterSquash
      sommerfugleBusk
      onion
      sunflower
      sunflowerVelvetQueen ]

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
