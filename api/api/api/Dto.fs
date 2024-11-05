module api.Dto

open System

type User =
    { id: domain.UserId
      needs: domain.PlantId List
      seeds: domain.PlantId List }

module User =
    let fromDomain (user: domain.User) =
        { id = user.id
          needs = user.wants |> Set.toList
          seeds = user.seeds |> Set.toList }

    let fromDomainAsync user =
        task {
            let! user = user
            return fromDomain user
        }

type AddWants = { plantId: Guid }

type AddSeeds = { plantId: Guid }
