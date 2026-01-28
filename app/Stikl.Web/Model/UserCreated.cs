namespace Stikl.Web.Model;

public record UserCreated(
    string UserName,
    string GivenName,
    string FamilyName,
    string LocationLabel
) : UserEventPayload
{
    public override User Apply(User user) =>
        throw new InvalidOperationException("Cannot apply user created!");

    public User Create(UserEvent @event) =>
        new User(
            Email: @event.Email,
            UserName: UserName,
            GivenName: GivenName,
            FamilyName: FamilyName,
            Created: @event.Timestamp,
            Updated: @event.Timestamp,
            History: [@event]
        );
}
