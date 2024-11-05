module api.test.Http

open System.Net.Http
open System.Net.Http.Json

let get (path: string) (client: HttpClient) = client.GetAsync(path)

let getJson<'T> (path: string) (client: HttpClient) = client.GetFromJsonAsync<'T>(path)

let post<'T> (path: string) (payload: 'T) (client: HttpClient) =
    client.PostAsJsonAsync<'T>(path, payload)
