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
    ImmutableHashSet<SpeciesId> Wants
);
