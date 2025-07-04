module webapp.Composition

open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open Npgsql
open Services
open domain
open webapp.Data.Inmemory
open webapp.Data.postgres


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


let seedsOf plant =
    { plant = plant
      comment = None
      seedKind = Seed }

let cuttingOf plant =
    { plant = plant
      comment = None
      seedKind = Cutting }

let diceImg seed =
    $"https://api.dicebear.com/9.x/shapes/svg?seed={seed}"


let users =
    // TODO: create test users from actual events!
    [ { username = Username "cabang"
        authId = Some "auth0|66bf4709cc2ef32cba53828a"
        firstName = Some "Casper"
        fullName = Some "Casper Bang"
        imgUrl = diceImg "cabang"
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
        authId = None
        firstName = Some "Bob"
        fullName = Some "Bob Jensen"
        imgUrl = diceImg "bob"
        wants = Set.singleton basil
        seeds = Set.singleton (cuttingOf lavender)
        history = List.empty }
      { username = Username "alice"
        authId = None
        firstName = Some "Alice"
        fullName = Some "Alice Adventure"
        imgUrl = diceImg "alice"
        wants = Set.ofList [ thyme; basil ]
        seeds = Set.empty
        history = List.empty } ]

type PlantRepository =
    { getAll: unit -> Plant List Task
      get: PlantId -> Plant Option Task
      exists: PlantId -> bool Task }

let inMemoryPlantRepository (entities: Plant List) =
    let mutable entities = entities

    let tryGet id =
        entities |> List.tryFind (fun entity -> entity.id = id)

    { getAll = fun () -> entities |> Task.FromResult
      get = tryGet >> Task.FromResult
      exists = tryGet >> Option.isSome >> Task.FromResult }


let register (service: 'a) (services: IServiceCollection) =
    services.AddSingleton<'a>(service) |> ignore

    services

let registerPostgresDataSource(services: IServiceCollection) =
    services.AddSingleton<NpgsqlDataSource>(fun s ->
                          let connectionStringBuilder = NpgsqlConnectionStringBuilder()
                          connectionStringBuilder.Host <- "localhost"
                          connectionStringBuilder.Port <- 5432
                          connectionStringBuilder.Username <- "postgres"
                          connectionStringBuilder.Password <- "" 
                          connectionStringBuilder.Database <- "stikl"
                          NpgsqlDataSourceBuilder(connectionStringBuilder.ToString())
                              .Build()
                          )

let registerUserRepository (users: User list) (services: IServiceCollection) =
    //services.AddSingleton<UserStore, InMemoryUserRepository>(fun _ -> InMemoryUserRepository(users))
    services.AddSingleton<UserStore, PostgresUserRepository>()

let registerPlantRepository (repository: PlantRepository) =
    register repository.get
    >> register repository.getAll
    >> register repository.exists
    >> register repository


let registerAll (services: IServiceCollection) =
    services
    |> registerUserRepository users
    |> registerPostgresDataSource
    |> registerPlantRepository (inMemoryPlantRepository plants)
