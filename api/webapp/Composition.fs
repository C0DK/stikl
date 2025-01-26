module webapp.Composition

open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open domain


// TODO: dont use guid for id :3
let basil =
    { id = PlantId "basil"
      name = "Basilikum"
      image_url = "https://bs.plantnet.org/image/o/3762433643a1e7307243999389cb652b85737c57" }

let lavender =
    { id = PlantId "topped_lavender"
      name = "Topped Lavender"
      image_url = "https://bs.plantnet.org/image/o/034910084fec5cbd4c2635d6549212636fb8fdf2" }

let sommerfugleBusk =
    { id = PlantId "buddleia_black_night"
      name = "Sommerfuglebusk, Buddleia dav. 'Black Knight'"
      image_url =
        "https://media.plantorama.dk/cdn/2SP93k/sommerfuglebusk-buddleia-dav-black-knight-5-liter-potte-sommerfuglebusk-buddleia-dav-black-knight-5-liter-potte.webp?d=14378" }

let winterSquash =
    { id = PlantId "winter_squash"
      name = "Vinter squash"
      image_url = "https://www.gardenia.net/wp-content/uploads/2023/05/cucurbita-maxima-winter-squash-780x520.webp" }

let pepperMint =
    { id = PlantId "peppermint"
      name = "Peppermynte"
      image_url = "https://www.gardenia.net/wp-content/uploads/2023/05/mentha-piperita-peppermint-780x520.webp" }

let rosemary =
    { id = PlantId "rosemary"
      name = "Rosemarin"
      image_url = "https://www.gardenia.net/wp-content/uploads/2023/05/rosmarinus-officinalis-arp-780x520.webp" }

let thyme =
    { id = PlantId "thyme"
      name = "Timian"
      image_url = "https://www.gardenia.net/wp-content/uploads/2023/05/thymus-serpyllum-creeping-thyme-780x520.webp" }

let onion =
    { id = PlantId "onion"
      name = "Løg"
      image_url = "https://www.gardenia.net/wp-content/uploads/2023/05/allium-cepa-780x520.webp" }

let sunflowerVelvetQueen =
    { id = PlantId "sunflower_velvet_queen"
      name = "Solsikke (Velvet Queen)"
      image_url = "https://www.gardenia.net/wp-content/uploads/2024/01/shutterstock_2049263999-800x533.jpg" }

let sunflower =
    { id = PlantId "sunflower"
      name = "Solsikke"
      image_url = "https://www.gardenia.net/wp-content/uploads/2023/05/Helianthus-annuus-504x533.webp" }

let dvaergTidsel =
    { id = PlantId "carline_tidsel"
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

// TODO: move to data access package
type UserDbo =
    { username: Username
      wants: Plant Set
      seeds: PlantOffer Set
      history: UserEvent List }

module UserDbo =
    let create (user: User) : UserDbo =
        { username = user.username
          wants = user.wants
          seeds = user.seeds
          history = user.history }

let seedsOf plant =
    { plant = plant
      comment = None
      seedKind = Seed }

let cuttingOf plant =
    { plant = plant
      comment = None
      seedKind = Cutting }

let users =
    [ { username = Username "cabang"
        wants = Set.ofList [ sunflowerVelvetQueen; thyme ]
        seeds =
          Set.ofList
              [ cuttingOf basil
                seedsOf winterSquash
                { cuttingOf rosemary with
                    comment = Some "Skal selv graves op" }
                cuttingOf pepperMint ]
        history = List.empty }
      { username = Username "bob"
        wants = Set.singleton basil
        seeds = Set.singleton (cuttingOf lavender)
        history = List.empty }
      { username = Username "alice"
        wants = Set.ofList [ thyme; basil ]
        seeds = Set.empty
        history = List.empty } ]

type PlantRepository =
    { getAll: unit -> Plant List Task
      get: PlantId -> Plant Option Task
      exists: PlantId -> bool Task }

type UserStore =
    { getAll: unit -> UserDbo List Task
      get: Username -> UserDbo Option Task
      create: Username -> Result<unit, string> Task
      applyEvent: UserEvent -> Username -> Result<UserEvent, string> Task }

let inMemoryPlantRepository (entities: Plant List) =
    let mutable entities = entities

    let tryGet id =
        entities |> List.tryFind (fun entity -> entity.id = id)

    { getAll = fun () -> entities |> Task.FromResult
      get = tryGet >> Task.FromResult
      exists = tryGet >> Option.isSome >> Task.FromResult }

let inMemoryUserRepository (users: UserDbo List) =

    let mutable users = users

    let toDom (user: UserDbo) : User =
        { username = user.username
          // this is fine for now but might be broken eventually.
          imgUrl = ""
          firstName = None
          fullName = None
          wants = user.wants
          seeds = user.seeds
          history = user.history }

    let updateUser func username =
        users <-
            users
            |> List.map (function
                | user when user.username = username -> (toDom user) |> func |> UserDbo.create
                | user -> user)

    let tryGetUser id =
        users |> List.tryFind (fun user -> user.username = id)

    { getAll = fun () -> users |> Task.FromResult
      get = tryGetUser >> Task.FromResult
      create =
        fun id ->
            match tryGetUser id with
            | Some _ -> Error "User Already Exists!" |> Task.FromResult
            | None ->
                users <- (UserDbo.create (User.create id)) :: users
                Ok() |> Task.FromResult
      applyEvent =
        (fun event username ->
            (
            // This get might be irrelevant, but it's to ensure that it fails.
            match tryGetUser username with
            | Some user ->
                do updateUser (apply event) user.username

                Ok event |> Task.FromResult
            | None -> Error $"User '{username}' Not Found" |> Task.FromResult)) }



let register (service: 'a) (services: IServiceCollection) =
    services.AddSingleton<'a>(service) |> ignore

    services


let registerUserRepository (repository: UserStore) =
    register repository.get
    >> register repository.getAll
    >> register repository.applyEvent
    >> register repository.create
    >> register repository

let registerPlantRepository (repository: PlantRepository) =
    register repository.get
    >> register repository.getAll
    >> register repository.exists
    >> register repository


let registerAll (services: IServiceCollection) =
    services
    |> registerUserRepository (inMemoryUserRepository users)
    |> registerPlantRepository (inMemoryPlantRepository plants)
