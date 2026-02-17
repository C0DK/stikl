using System.Text.Json.Serialization;

namespace Stikl.Web.Model;

public record UserCreated(
    Email Email,
    string FirstName,
    string LastName,
    // TODO: dont use the Dto directly plz.
    LocationIQClient.Location Location
) : UserEventPayload
{
    [JsonIgnore]
    public const string Kind = "user_created";

    [JsonIgnore]
    public override string EventKind => Kind;

    public override User Apply(User user) =>
        throw new InvalidOperationException("Cannot apply user created!");

    public User Create(UserEvent @event) =>
        new User(
            Email: Email,
            UserName: @event.Username,
            FirstName: FirstName,
            LastName: LastName,
            Location: Location,
            Created: @event.Timestamp,
            Updated: @event.Timestamp,
            History: [@event],
            Wants: []
        );
}
