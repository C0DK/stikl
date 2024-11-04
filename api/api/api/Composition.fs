module api.Composition

open System
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open domain


let basil = Guid.Parse "1265C604-6AD6-4102-8E36-8DA97D25DE8A"


let users =
    [ { id = Guid.Parse("C57485A6-BA3F-4226-842F-0D4C3691F019")
        needs = Set.empty
        seeds = Set.singleton basil
        history = List.empty }
      { id = Guid.Parse("28F09CC0-4CDC-40AD-B733-78CFF77829A3")
        needs = Set.singleton basil
        seeds = Set.empty
        history = List.empty } ]

let getUsers () = users |> Task.FromResult

let getUser id =
    let hasId user = user.id = id

    users |> List.tryFind hasId |> Task.FromResult

let register (services: IServiceCollection) =
    services.AddSingleton(getUser) |> ignore
    services.AddSingleton(getUsers) |> ignore
