using Flurl;
using Flurl.Http;

public class LocationIQClient(string apiKey)
{
    private const string BaseUrl = "https://eu1.locationiq.com";

    public async ValueTask<Location> Get(string osmId, CancellationToken cancellationToken) =>
        (
            await BaseUrl
                .AppendPathSegment("/v1/lookup")
                .AppendQueryParam(
                    new
                    {
                        osm_ids = "N" + osmId, // TODO: prefix better?
                        key = apiKey,
                        normalizecity = true,
                    }
                )
                .GetJsonAsync<Location[]>()
        ).Single();

    public async ValueTask<SearchResult[]> AutoComplete(
        string query,
        CancellationToken cancellationToken
    ) =>
        await BaseUrl
            .AppendPathSegment("/v1/autocomplete")
            .AppendQueryParam(
                new
                {
                    q = query,
                    key = apiKey,
                    tag = "place:city,place:town,place:village,place:borough,place:hamlet,place:quarter,place:heighbourhood,place:suburb",
                }
            )
            .GetJsonAsync<SearchResult[]>();

    public record Address(
        string Country,
        string CountryCode,
        string? Name = null,
        string? Suburb = null,
        string? Village = null,
        string? Postcode = null,
        string? City = null,
        string? State = null
    )
    {
        public string? Label => Name ?? Suburb ?? Village ?? City;
    }

    public record Location(
        string PlaceId,
        string OsmId,
        string OsmType,
        string Licence,
        string Lat,
        string Lon,
        IReadOnlyList<string> Boundingbox,
        string Class,
        string Type,
        string DisplayName,
        Address Address
    );

    public record SearchResult(
        string PlaceId,
        string OsmId,
        string OsmType,
        string Licence,
        string Lat,
        string Lon,
        IReadOnlyList<string> Boundingbox,
        string Class,
        string Type,
        string DisplayName,
        string DisplayPlace,
        string DisplayAddress,
        Address Address
    )
        : Location(
            PlaceId,
            OsmId,
            OsmType,
            Licence,
            Lat,
            Lon,
            Boundingbox,
            Class,
            Type,
            DisplayName,
            Address
        );
}
