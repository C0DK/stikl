using System.Collections.Immutable;
using NUnit.Framework;
using Stikl.Web.Model;

namespace Stikl.Tests;

[TestFixture]
public class UserCreatedTests
{
    static readonly DateTimeOffset EventTime = new(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

    static LocationIQClient.Location MakeLocation() =>
        new(
            PlaceId: "place1",
            OsmId: "456",
            OsmType: "N",
            Licence: "",
            Lat: "51.5",
            Lon: "-0.1",
            Boundingbox: [],
            Class: "place",
            Type: "city",
            DisplayName: "London",
            Address: new("United Kingdom", "gb", Name: "London")
        );

    [Test]
    public void Create_ProducesUserWithCorrectFields()
    {
        var username = Username.Parse("bob");
        var email = Email.Parse("bob@example.com");
        var location = MakeLocation();

        var payload = new UserCreated(email, "Bob", "Smith", location);
        var @event = new UserEvent(username, 1, EventTime, payload);

        var user = payload.Create(@event);

        Assert.That(user.UserName, Is.EqualTo(username));
        Assert.That(user.Email, Is.EqualTo(email));
        Assert.That(user.FirstName, Is.EqualTo("Bob"));
        Assert.That(user.LastName, Is.EqualTo("Smith"));
        Assert.That(user.Location, Is.EqualTo(location));
        Assert.That(user.Created, Is.EqualTo(EventTime));
        Assert.That(user.Updated, Is.EqualTo(EventTime));
    }

    [Test]
    public void Create_StartsWithEmptyWantsAndHas()
    {
        var username = Username.Parse("carol");
        var payload = new UserCreated(
            Email.Parse("carol@example.com"),
            "Carol",
            "Jones",
            MakeLocation()
        );
        var @event = new UserEvent(username, 1, EventTime, payload);

        var user = payload.Create(@event);

        Assert.That(user.Wants, Is.Empty);
        Assert.That(user.Has, Is.Empty);
    }

    [Test]
    public void Create_InitialHistoryContainsCreationEvent()
    {
        var username = Username.Parse("dave");
        var payload = new UserCreated(
            Email.Parse("dave@example.com"),
            "Dave",
            "Brown",
            MakeLocation()
        );
        var @event = new UserEvent(username, 1, EventTime, payload);

        var user = payload.Create(@event);

        Assert.That(user.History, Has.Length.EqualTo(1));
        Assert.That(user.History[0], Is.EqualTo(@event));
    }

    [Test]
    public void Apply_Throws()
    {
        var username = Username.Parse("eve");
        var location = MakeLocation();
        var payload = new UserCreated(Email.Parse("eve@example.com"), "Eve", "White", location);

        // Apply should not be called on UserCreated — it always throws
        Assert.Throws<InvalidOperationException>(() => payload.Apply(null!));
    }

    [Test]
    public void EventKind_IsUserCreated()
    {
        var payload = new UserCreated(Email.Parse("x@x.com"), "X", "Y", MakeLocation());
        Assert.That(payload.EventKind, Is.EqualTo("user_created"));
    }
}
