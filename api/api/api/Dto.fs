module api.Dto

open System

type User =
    { id: string
      wants: domain.PlantId List
      seeds: domain.PlantId List }

module User =
    let fromDomain (user: domain.User) =
        { id = user.id.value
          wants = user.wants |> Set.toList
          seeds = user.seeds |> Set.toList }

    let fromDomainAsync user =
        task {
            let! user = user
            return fromDomain user
        }


type PlantRequest = { plantId: Guid }
