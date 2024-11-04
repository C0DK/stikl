module domain

open System

type PlantId = Guid
type UserId = Guid

type UserEvent =
    | Needs of PlantId
    | Seeds of PlantId
    | NoLongerNeeds of PlantId
    | NoLongerSeeds of PlantId


type User =
    { id: UserId
      needs: PlantId Set
      seeds: PlantId Set
      history: UserEvent List }

module User =
    let Wants plantId user = Set.contains plantId user.needs

    let Has plantId user = Set.contains plantId user.seeds

let apply (event: UserEvent) (user: User) =
    let user =
        (match event with
         | Needs plantId ->
             { user with
                 needs = Set.add plantId user.needs }
         | Seeds plantId ->
             { user with
                 seeds = Set.add plantId user.seeds }
         | NoLongerNeeds plantId ->
             { user with
                 needs = Set.remove plantId user.needs }
         | NoLongerSeeds plantId ->
             { user with
                 needs = Set.remove plantId user.seeds })

    { user with
        history = event :: user.history }
