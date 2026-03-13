using System.Text.Json.Serialization;

namespace Stikl.Web.Model;

public record WantPlant(SpeciesId plant) : UserEventPayload
{
    [JsonIgnore]
    public const string Kind = "want_plant";

    [JsonIgnore]
    public override string EventKind => Kind;

    public override User Apply(User user) => user with { Wants = user.Wants.Add(plant) };
}
