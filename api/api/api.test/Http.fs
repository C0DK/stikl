module api.test.Http

open System.Net.Http
open System.Net.Http.Json

let get (path: string) (client: HttpClient) = client.GetAsync(path)

let getJson<'T> (path: string) (client: HttpClient) = client.GetFromJsonAsync<'T>(path)

let postJson<'T> (path: string) (payload: 'T) (client: HttpClient) =
    client.PostAsJsonAsync<'T>(path, payload)

let postEmpty<'T> (path: string) (client: HttpClient) = client.PostAsync(path, null)

let loginAs (userId: domain.UserId) (client: HttpClient) =
    // TODO get correct token
    let token = userId.value
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}")
