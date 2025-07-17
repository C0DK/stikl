module Stikl.Web.Composition

open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open Npgsql
open Stikl.Web
open Stikl.Web.Components
open Stikl.Web.Pages
open Stikl.Web.services
open Stikl.Web.services.User
open domain
open Stikl.Web.Data.postgres


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

let registerPostgresDataSource (services: IServiceCollection) =
    services.AddSingleton<NpgsqlDataSource>(fun s ->
        let connectionStringBuilder = NpgsqlConnectionStringBuilder()
        connectionStringBuilder.Host <- EnvironmentVariable.getRequired "DB_HOST"
        connectionStringBuilder.Port <- 5432
        connectionStringBuilder.Username <- "postgres"
        connectionStringBuilder.Password <- ""
        connectionStringBuilder.Database <- "stikl"
        NpgsqlDataSourceBuilder(connectionStringBuilder.ToString()).Build())

let registerEventHandler (services: IServiceCollection) =
    services.AddTransient<EventHandler>(fun s ->
        let store = s.GetRequiredService<UserStore>()
        let identity = s.GetRequiredService<CurrentUser>()
        let eventBroker = s.GetRequiredService<EventBroker.EventBroker>()
        // TODO: use composition variant and move that too.
        { handle =
            (fun eventPayload ->
                let apply username =
                    (UserEvent.create eventPayload username)
                    |> store.ApplyEvent
                    |> Task.collect (
                        Result.map (fun e ->
                            task {
                                do! eventBroker.Publish e CancellationToken.None
                                return e
                            })
                        >> Task.unpackResult
                    )

                match identity with
                | AuthedUser user ->
                    match eventPayload with
                    | CreateUser _ -> Task.FromResult(Error "You cannot create user twice")
                    | _ -> apply user.username
                | Anonymous -> Task.FromResult(Error "Cannot do things if you aren't logged in")
                | NewUser _ ->
                    match eventPayload with
                    | CreateUser createUser -> apply createUser.username
                    | _ -> Task.FromResult(Error "You cannot do that until your user is created")) }
        : EventHandler)

let registerUserRepository (services: IServiceCollection) =
    services.AddSingleton<UserStore, PostgresUserRepository>()

let registerPlantRepository (repository: PlantRepository) =
    register repository.get
    >> register repository.getAll
    >> register repository.exists
    >> register repository


let registerAll : IServiceCollection -> IServiceCollection =
    registerUserRepository
    >> registerPostgresDataSource
    >> registerEventHandler
    >> User.register
    >> Layout.register
    >> PlantCard.register
    >> EventBroker.register
    >> registerPlantRepository (inMemoryPlantRepository plants)
