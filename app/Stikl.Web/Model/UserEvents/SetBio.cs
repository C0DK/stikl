using System.Text.Json.Serialization;

namespace Stikl.Web.Model;

public record SetBio(string? Bio) : UserEventPayload
{
    [JsonIgnore]
    public const string Kind = "set_bio";

    public override string EventKind => Kind;

    public override User Apply(User user) => user with { Bio = Bio };
}
