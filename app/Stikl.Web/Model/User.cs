using System.Collections.Immutable;

namespace Stikl.Web.Model;

public record User(
    string UserName,
    Email Email,
    string GivenName,
    string FamilyName,
    DateTimeOffset Created,
    DateTimeOffset Updated,
    ImmutableArray<UserEvent> History
);
