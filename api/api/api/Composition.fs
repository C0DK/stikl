module api.Composition

open System
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open domain


let basil = Guid.Parse "1265C604-6AD6-4102-8E36-8DA97D25DE8A"


let users =
    [ { id = Guid.Parse("C57485A6-BA3F-4226-842F-0D4C3691F019")
        wants = Set.empty
        seeds = Set.singleton basil
        history = List.empty }
      { id = Guid.Parse("28F09CC0-4CDC-40AD-B733-78CFF77829A3")
        wants = Set.singleton basil
        seeds = Set.empty
        history = List.empty } ]

type UserRepository =
    { getUsers: unit -> User List Task
      getUser: UserId -> User Option Task }

let inMemoryUserProvider (users: User List) =

    { getUsers = fun () -> users |> Task.FromResult
      getUser = fun id -> users |> List.tryFind (fun user -> user.id = id) |> Task.FromResult
    }



let register (service: 'a) (services: IServiceCollection) =
    services.AddSingleton<'a>(service) |> ignore

    services

let registerUserRepository (provider: UserRepository) =
    register provider.getUser >> register provider.getUsers

let registerAll (services: IServiceCollection) =
    services |> registerUserRepository (inMemoryUserProvider users) |> ignore
