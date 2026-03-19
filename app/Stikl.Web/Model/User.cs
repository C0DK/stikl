using System.Collections.Immutable;

namespace Stikl.Web.Model;

public record User(
    Username UserName,
    Email Email,
    string FirstName,
    string LastName,
    LocationIQClient.Location Location,
    DateTimeOffset Created,
    DateTimeOffset Updated,
    ImmutableArray<UserEvent> History,
    ImmutableHashSet<SpeciesId> Wants,
    ImmutableHashSet<PlantOffer> Has,
    string? Bio = null
)
{
    public bool DoesHas(SpeciesId id) => Has.Any(p => p.Id == id);

    public bool DoesWant(SpeciesId id) => Wants.Any(p => p == id);
}
