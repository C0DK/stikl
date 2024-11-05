module domain

open System

type PlantId = Guid
type UserId = Guid

type UserEvent =
    | AddedWant of PlantId
    | AddedSeeds of PlantId
    | RemovedWant of PlantId
    | RemovedSeeds of PlantId

type User =
    { id: UserId
      wants: PlantId Set
      seeds: PlantId Set
      history: UserEvent List }

module User =
    let Wants plantId user = Set.contains plantId user.wants

    let Has plantId user = Set.contains plantId user.seeds
    
    let GetWants user = user.wants
    
    let GetSeeds user = user.seeds

    let createNew () =
        { id = Guid.NewGuid()
          wants = Set.empty
          seeds = Set.empty
          history = List.empty }

let apply (event: UserEvent) (user: User) =
    let user =
        (match event with
         | AddedWant plantId ->
             { user with
                 wants = Set.add plantId user.wants }
         | AddedSeeds plantId ->
             { user with
                 seeds = Set.add plantId user.seeds }
         | RemovedWant plantId ->
             { user with
                 wants = Set.remove plantId user.wants }
         | RemovedSeeds plantId ->
             { user with
                 seeds = Set.remove plantId user.seeds })

    { user with
        history = event :: user.history }

