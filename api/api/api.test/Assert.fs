module api.test.Assert

open System.Net
open System.Net.Http
open System.Threading.Tasks
open Xunit

let equal (a: 'T) (b: 'T) = Assert.Equal<'T>(a, b)

let hasStatusCode (expected: HttpStatusCode) (response: HttpResponseMessage Task) =
    task {
        let! response = response
        equal expected response.StatusCode
    }

let hasStatusCodeOk response =
    hasStatusCode HttpStatusCode.OK response
