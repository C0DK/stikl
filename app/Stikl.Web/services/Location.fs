module Stikl.Web.services.Location

open System.Net.Http
open System.Net.Http.Json
open System.Text.Json.Serialization
open System.Threading
open System.Threading.Tasks
open System.Web
open Microsoft.Extensions.DependencyInjection
open domain

type private KommuneDto = { navn: string }

type private StedDto =
    { id: string
      primærtnavn: string
      undertype: string
      visueltcenter: decimal list
      kommuner: KommuneDto list }

type private ResultDto = { navn: string; sted: StedDto }

type DawaLocation = { id: string; location: Location }

type LocationService(client: HttpClient) =
    
    let jsonSerializerOptions =
        JsonFSharpOptions
            .Default()
            // Add any .WithXXX() calls here to customize the format
            .ToJsonSerializerOptions()

    // Only supports Denmark
    member this.Query (query: string) (cancellationToken: CancellationToken) : DawaLocation list Task =
        task {
            // TODO: sanitize query when user defined.
            let query = HttpUtility.UrlEncode query
            let pageSize = 10

            let! resp =
                client.GetAsync(
                    $"https://api.dataforsyningen.dk/stednavne2?q={query}&hovedtype=Bebyggelse&fuzzy&per_side={pageSize}", cancellationToken
                )


            let! entities = resp.Content.ReadFromJsonAsync<ResultDto list>(cancellationToken= cancellationToken, options=jsonSerializerOptions)

            return
                entities
                |> List.map (fun dto ->
                    let kommuneNavn = dto.sted.kommuner.Head.navn
                    let primaryName =  dto.sted.primærtnavn
                    let label = if primaryName = kommuneNavn then primaryName else $"{primaryName} ({kommuneNavn})"
                    { id = dto.sted.id
                      location =
                        { label = label
                          lat = dto.sted.visueltcenter[1]
                          lon = dto.sted.visueltcenter[0] } })
        }
let register : IServiceCollection -> IServiceCollection = 
    Services.registerSingletonType<LocationService>
    >> Services.registerHttpClient<LocationService>
