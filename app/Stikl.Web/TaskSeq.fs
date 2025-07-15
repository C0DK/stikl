module Stikl.Web.TaskSeq

open System.Threading.Tasks
open FSharp.Control

let eachAsync (f: 'a -> Task) (vs: 'a TaskSeq) =
    task {
        for v in vs do
            do! f v
    }

let collectTask (f: 'a -> 'b TaskSeq) (lazyA: 'a Task) =
    taskSeq {
        let! a = lazyA

        for v in f a do
            yield v
    }
