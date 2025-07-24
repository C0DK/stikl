module Stikl.Web.services.Location

open System
open System.Net
open System.Net.Http
open System.Net.Http.Json
open System.Text.Json.Serialization
open System.Threading
open System.Threading.Tasks
open System.Web
open Microsoft.Extensions.DependencyInjection
open domain
open Stikl.Web

type private KommuneDto = { navn: string }

type private StedDto =
    { id: string
      primærtnavn: string
      undertype: string
      visueltcenter: decimal list
      kommuner: KommuneDto list }

type private ResultDto = { navn: string; sted: StedDto }

// Only supports Denmark
type LocationService(client: HttpClient) =

    let jsonSerializerOptions = JsonFSharpOptions.Default().ToJsonSerializerOptions()

    let map (dto: StedDto) =
        let kommuneNavn = dto.kommuner.Head.navn
        let primaryName = dto.primærtnavn

        let label =
            if primaryName = kommuneNavn then
                primaryName
            else
                $"{primaryName} ({kommuneNavn})"

        { id = Guid dto.id
          location =
            { label = label
              lat = dto.visueltcenter[1]
              lon = dto.visueltcenter[0] } }

    member this.get (id: Guid) (cancellationToken: CancellationToken) : Result<DawaLocation, string> Task =
        task {
            let! resp = client.GetAsync($"https://api.dataforsyningen.dk/steder/{id}", cancellationToken)

            if (resp.StatusCode = HttpStatusCode.NotFound) then
                return Error "Not Found"
            else
                let! dto =
                    resp.Content.ReadFromJsonAsync<StedDto>(
                        cancellationToken = cancellationToken,
                        options = jsonSerializerOptions
                    )

                return Ok(map dto)
        }

    member this.Query (query: string) (cancellationToken: CancellationToken) : DawaLocation list Task =
        // TODO: sanitize query when user defined.
        let query = HttpUtility.UrlEncode query
        let pageSize = 10

        client.GetAsync(
            $"https://api.dataforsyningen.dk/stednavne2?q={query}&hovedtype=Bebyggelse&fuzzy&per_side={pageSize}",
            cancellationToken
        )
        |> Task.collect
            _.Content.ReadFromJsonAsync<ResultDto list>(
                cancellationToken = cancellationToken,
                options = jsonSerializerOptions
            )
        |> Task.map (List.map (_.sted >> map))


let register: IServiceCollection -> IServiceCollection =
    Services.registerSingletonType<LocationService>
    >> Services.registerHttpClient<LocationService>
