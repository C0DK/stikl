using System.Text.Json.Serialization;

namespace Stikl.Web.Model;

public record HasPlant(SpeciesId Species, PlantOfferType Type, string? Comment) : UserEventPayload
{
    [JsonIgnore]
    public const string Kind = "has_plant";

    [JsonIgnore]
    public override string EventKind => Kind;

    public override User Apply(User user) =>
        user with
        {
            Has = user.Has.Add(
                new PlantOffer(Species, Type, string.IsNullOrWhiteSpace(Comment) ? null : Comment)
            ),
        };
}
