using System.Text.Json.Serialization;

namespace Stikl.Web.Model;

public record AccountDeleted() : UserEventPayload
{
    [JsonIgnore]
    public const string Kind = "account_deleted";

    public override string EventKind => Kind;

    public override User Apply(User user) => user;
}
