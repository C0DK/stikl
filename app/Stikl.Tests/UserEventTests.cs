using System.Collections.Immutable;
using NUnit.Framework;
using Stikl.Web.Model;

namespace Stikl.Tests;

[TestFixture]
public class UserEventTests
{
    static readonly DateTimeOffset BaseTime = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    static LocationIQClient.Location MakeLocation() =>
        new(
            PlaceId: "1",
            OsmId: "123",
            OsmType: "N",
            Licence: "",
            Lat: "0",
            Lon: "0",
            Boundingbox: [],
            Class: "place",
            Type: "city",
            DisplayName: "Testville",
            Address: new("Testland", "tl", Name: "Testville")
        );

    static User MakeUser(Username username) =>
        new UserCreated(
            Email: Email.Parse("user@example.com"),
            FirstName: "Test",
            LastName: "User",
            Location: MakeLocation()
        ).Create(
            new UserEvent(
                Username: username,
                Version: 1,
                Timestamp: BaseTime,
                Payload: null!
            )
        );

    [Test]
    public void Apply_UpdatesHistoryAndTimestamp()
    {
        var username = Username.Parse("alice");
        var user = MakeUser(username);
        var laterTime = BaseTime.AddHours(1);
        var speciesId = new SpeciesId(42);
        var payload = new WantPlant(speciesId);
        var @event = new UserEvent(username, 2, laterTime, payload);

        var updated = @event.Apply(user);

        Assert.That(updated.History.Length, Is.EqualTo(2));
        Assert.That(updated.History[1], Is.EqualTo(@event));
        Assert.That(updated.Updated, Is.EqualTo(laterTime));
    }

    [Test]
    public void Apply_DoesNotMutateOriginalUser()
    {
        var username = Username.Parse("alice");
        var user = MakeUser(username);
        var original = user;
        var payload = new WantPlant(new SpeciesId(1));
        var @event = new UserEvent(username, 2, BaseTime, payload);

        @event.Apply(user);

        Assert.That(user, Is.EqualTo(original));
    }
}
