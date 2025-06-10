module webapp.TaskSeq

open System.Threading.Tasks
open FSharp.Control

let eachAsync (f: 'a -> Task) (vs: 'a TaskSeq) =
    task {
        for v in vs do
            do! f v
    }
