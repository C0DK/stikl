module Stikl.Web.services.Cache

open System.Threading.Tasks
open Microsoft.Extensions.Caching.Memory

let getOrCreateAsync (key:string) (factory: unit ->'a Task)  (cache: IMemoryCache)=
    cache.GetOrCreateAsync(key, fun _ -> factory ())

