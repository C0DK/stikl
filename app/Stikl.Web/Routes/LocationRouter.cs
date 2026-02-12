using Microsoft.AspNetCore.Mvc;
using Stikl.Web.Templates.Components;

namespace Stikl.Web.Routes;

public class LocationRouter
{
    public static void Map(IEndpointRouteBuilder builder)
    {
        builder.MapGet(
            "form/search",
            async (
                LocationIQClient locationIq,
                ILogger logger,
                string locationQuery,
                CancellationToken cancellationToken
            ) =>
            {
                // dont 404 on missing results..
                var suggestions = await locationIq.AutoComplete(locationQuery, cancellationToken);
                logger.ForContext("entries", suggestions, true).Debug("Got suggestions");
                return string.Join(
                    "\n",
                    suggestions.Select(suggestion => new LocationSuggestion(
                        osmId: suggestion.OsmId,
                        label: suggestion.DisplayPlace,
                        address: suggestion.DisplayName
                    ))
                );
            }
        );
        builder.MapGet(
            "form/select/{osmId}",
            async (
                LocationIQClient locationIq,
                [FromRoute] string osmId,
                ILogger logger,
                CancellationToken cancellationToken
            ) =>
            {
                var location = await locationIq.Get(osmId, cancellationToken);
                logger.ForContext("location", location, true).Debug("Got suggestions");
                // TODO: check if user already has user.
                return new LocationSelection(
                    osmId: location.OsmId,
                    label: location.Address.Label ?? location.DisplayName,
                    address: location.DisplayName
                ).ToString();
            }
        );
    }
}
