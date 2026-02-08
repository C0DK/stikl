using System.Text.Json.Serialization;

namespace Stikl.Web.Model;

public record UserCreated(Email Email, string FirstName, string LastName, string LocationLabel)
    : UserEventPayload
{
    [JsonIgnore]
    public const string Kind = "user_created";

    public override User Apply(User user) =>
        throw new InvalidOperationException("Cannot apply user created!");

    public User Create(UserEvent @event) =>
        new User(
            Email: Email,
            UserName: @event.Username,
            FirstName: FirstName,
            LastName: LastName,
            Location: LocationLabel,
            Created: @event.Timestamp,
            Updated: @event.Timestamp,
            History: [@event]
        );
}
