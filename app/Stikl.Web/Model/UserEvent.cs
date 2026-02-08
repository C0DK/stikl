namespace Stikl.Web.Model;

public record UserEvent(
    Username Username,
    int Version,
    DateTimeOffset Timestamp,
    UserEventPayload Payload
)
{
    public User Apply(User user) =>
        Payload.Apply(user) with
        {
            History = user.History.Add(this),
            Updated = Timestamp,
        };
}
