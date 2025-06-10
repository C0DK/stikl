module webapp.ValueTask

open System.Threading.Tasks

let whenAll (ts: ValueTask seq)  =
    task {
        for t in ts do
            do! t
    }

let map (f: 'a -> 'b) (t : 'a ValueTask) =
    task {
        let! v = t
        return f v
    }
    
