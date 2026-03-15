using Stikl.Web.Model;

namespace Stikl.Tests;

/// <summary>
/// Shared factory for creating test User instances without a database.
/// </summary>
internal static class UserFactory
{
    static readonly DateTimeOffset DefaultTime = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    static LocationIQClient.Location DefaultLocation() =>
        new(
            PlaceId: "1",
            OsmId: "1",
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

    public static User Create(
        Username? username = null,
        Email? email = null,
        string firstName = "Test",
        string lastName = "User",
        LocationIQClient.Location? location = null,
        DateTimeOffset? timestamp = null
    )
    {
        var u = username ?? Username.Parse("testuser");
        var e = email ?? Email.Parse("test@example.com");
        var loc = location ?? DefaultLocation();
        var ts = timestamp ?? DefaultTime;

        var payload = new UserCreated(e, firstName, lastName, loc);
        var @event = new UserEvent(u, 1, ts, payload);
        return payload.Create(@event);
    }
}
