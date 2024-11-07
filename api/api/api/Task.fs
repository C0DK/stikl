module api.Task

let map func t =
    task {
        let! value = t

        return func value
    }

let collect func t =
    task {
        let! value = t

        return! func value
    }
