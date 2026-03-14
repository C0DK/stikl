using NUnit.Framework;
using Stikl.Web.Model;

namespace Stikl.Tests;

[TestFixture]
public class UserEventPayloadSerializationTests
{
    [Test]
    public void WantPlant_RoundTrip()
    {
        var original = new WantPlant(new SpeciesId(42));
        var json = original.Serialize();
        var deserialized = UserEventPayload.Deserialize(json);

        Assert.That(deserialized, Is.InstanceOf<WantPlant>());
        var result = (WantPlant)deserialized;
        Assert.That(result.plant.Value, Is.EqualTo(42));
    }

    [Test]
    public void UnwantPlant_RoundTrip()
    {
        var original = new UnwantPlant(new SpeciesId(7));
        var json = original.Serialize();
        var deserialized = UserEventPayload.Deserialize(json);

        Assert.That(deserialized, Is.InstanceOf<UnwantPlant>());
        var result = (UnwantPlant)deserialized;
        Assert.That(result.plant.Value, Is.EqualTo(7));
    }

    [Test]
    public void HasPlant_RoundTrip()
    {
        var original = new HasPlant(new SpeciesId(15), PlantOfferType.Sapling, "Lovely plant");
        var json = original.Serialize();
        var deserialized = UserEventPayload.Deserialize(json);

        Assert.That(deserialized, Is.InstanceOf<HasPlant>());
        var result = (HasPlant)deserialized;
        Assert.That(result.Species.Value, Is.EqualTo(15));
        Assert.That(result.Type, Is.EqualTo(PlantOfferType.Sapling));
        Assert.That(result.Comment, Is.EqualTo("Lovely plant"));
    }

    [Test]
    public void Serialize_IncludesKindDiscriminator_WantPlant()
    {
        var payload = new WantPlant(new SpeciesId(1));
        var json = payload.Serialize();
        Assert.That(json, Does.Contain("want_plant"));
    }

    [Test]
    public void Serialize_IncludesKindDiscriminator_UnwantPlant()
    {
        var payload = new UnwantPlant(new SpeciesId(1));
        var json = payload.Serialize();
        Assert.That(json, Does.Contain("unwant_plant"));
    }

    [Test]
    public void Serialize_IncludesKindDiscriminator_HasPlant()
    {
        var payload = new HasPlant(new SpeciesId(1), PlantOfferType.Seed, null);
        var json = payload.Serialize();
        Assert.That(json, Does.Contain("has_plant"));
    }
}
