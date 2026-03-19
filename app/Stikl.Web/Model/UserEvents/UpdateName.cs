using System.Text.Json.Serialization;

namespace Stikl.Web.Model;

public record UpdateName(string FirstName, string LastName) : UserEventPayload
{
    [JsonIgnore]
    public const string Kind = "update_name";

    public override string EventKind => Kind;

    public override User Apply(User user) =>
        user with
        {
            FirstName = FirstName,
            LastName = LastName,
        };
}
