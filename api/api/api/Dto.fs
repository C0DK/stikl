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

type PlantSummary = { id: string; name: string }

type Plant =
    { id: string
      name: string
      image_url: string }


module Plant =
    let fromDomain (dom: domain.Plant) =
        { id = dom.id.ToString()
          name = dom.name
          image_url = dom.image_url }

module PlantSummary =
    let fromDomain (dom: domain.Plant) =
        { id = dom.id.ToString()
          name = dom.name }

type PlantRequest = { plantId: Guid }
