module api.test.Fake

open Microsoft.AspNetCore.Mvc.Testing
open api
open Microsoft.Extensions.DependencyInjection


type FakeApi(configureServices: IServiceCollection -> unit) =
    // It has to bind to a random class in the correct project, to work
    inherit WebApplicationFactory<api.Controllers.UserController>()


    override this.ConfigureWebHost builder =
        builder.ConfigureServices configureServices |> ignore


let setCleanRepository builder = builder

let getClientWithDependencies configureServices =
    let api = new FakeApi(configureServices)
    api.CreateClient()

let getClientWithModelRepository repo =
    getClientWithDependencies ((Composition.addInMemoryModelRepository repo) >> ignore)

let getClient () =
    let repo = InMemory.ModelRepository.withModels Fixture.models

    getClientWithModelRepository repo

module Http =
    open System.Net.Http
    open System.Text
    open System.Text.Json

    let serialize payload =
        JsonSerializer.Serialize(payload, CompositionRoot.jsonSerializerOptions)

    let deserialize<'T> (content: string) =
        printf $"CONTENT = {content}"

        try
            JsonSerializer.Deserialize<'T>(content, CompositionRoot.jsonSerializerOptions)
        with :? JsonException as exc ->
            failwith $"Invalid json! ({exc.Message})"



    let getPayload<'T> (response: HttpResponseMessage) =
        response.Content.ReadAsStringAsync() |> Task.map deserialize<'T>

    let getSuccessfulPayload<'T> (response: HttpResponseMessage) =
        if not response.IsSuccessStatusCode then
            failwith $"Expected success(2XX), was {response.StatusCode}"

        getPayload<'T> response

    let postPayload<'T> (endpoint: string) (payload: 'T) (client: HttpClient) =
        let json = serialize payload

        let content = new StringContent(json, Encoding.UTF8, "application/json")

        client.PostAsync(endpoint, content)
