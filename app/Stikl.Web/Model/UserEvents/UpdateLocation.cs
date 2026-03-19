using System.Text.Json.Serialization;

namespace Stikl.Web.Model;

public record UpdateLocation(LocationIQClient.Location Location) : UserEventPayload
{
    [JsonIgnore]
    public const string Kind = "update_location";

    public override string EventKind => Kind;

    public override User Apply(User user) => user with { Location = Location };
}
