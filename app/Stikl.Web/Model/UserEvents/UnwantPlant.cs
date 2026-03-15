using System.Text.Json.Serialization;

namespace Stikl.Web.Model;

public record UnwantPlant(SpeciesId plant) : UserEventPayload
{
    [JsonIgnore]
    public const string Kind = "unwant_plant";

    public override string EventKind => Kind;

    public override User Apply(User user) => user with { Wants = user.Wants.Remove(plant) };
}
