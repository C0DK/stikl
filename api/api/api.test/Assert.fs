module api.test.Assert

open System.Net
open System.Net.Http
open System.Threading.Tasks
open Xunit
open api

let equal (a: 'T) (b: 'T) = Assert.Equal<'T>(a, b)

let hasStatusCode (expected: HttpStatusCode) (response: HttpResponseMessage) =
    Assert.True(
        (expected = response.StatusCode),
        $"""Expected {expected}, got {response.StatusCode}
            Content: '{response.Content.ReadAsStringAsync().Result}
            Headers: '{response.Headers}'
            """
    )

let asyncHasStatusCode (expected: HttpStatusCode) = Task.map (hasStatusCode expected)

let hasStatusCodeOk response =
    asyncHasStatusCode HttpStatusCode.OK response
