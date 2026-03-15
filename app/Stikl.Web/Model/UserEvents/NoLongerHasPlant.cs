using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Stikl.Web.Model;

public record NoLongerHasPlant(SpeciesId Species) : UserEventPayload
{
    [JsonIgnore]
    public const string Kind = "no_longer_has_plant";

    [JsonIgnore]
    public override string EventKind => Kind;

    public override User Apply(User user) =>
        user with
        {
            Has = user.Has.Where(p => p.Id != Species).ToImmutableHashSet(),
        };
}
