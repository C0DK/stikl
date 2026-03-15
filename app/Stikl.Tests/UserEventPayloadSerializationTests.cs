using NUnit.Framework;
using Stikl.Web.Model;

namespace Stikl.Tests;

[TestFixture]
public class UserEventPayloadSerializationTests
{
    [TestFixture]
    public class RoundTrip
    {
        [Test]
        public void WantPlant()
        {
            var original = new WantPlant(new SpeciesId(42));
            var json = original.Serialize();
            var deserialized = UserEventPayload.Deserialize(json);

            Assert.That(deserialized, Is.InstanceOf<WantPlant>());
            Assert.That(((WantPlant)deserialized).plant.Value, Is.EqualTo(42));
        }

        [Test]
        public void UnwantPlant()
        {
            var original = new UnwantPlant(new SpeciesId(7));
            var json = original.Serialize();
            var deserialized = UserEventPayload.Deserialize(json);

            Assert.That(deserialized, Is.InstanceOf<UnwantPlant>());
            Assert.That(((UnwantPlant)deserialized).plant.Value, Is.EqualTo(7));
        }

        [Test]
        public void HasPlant()
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
    }

    [TestFixture]
    public class KindDiscriminator
    {
        [Test]
        public void WantPlant_IncludesKind()
        {
            var json = new WantPlant(new SpeciesId(1)).Serialize();
            Assert.That(json, Does.Contain("want_plant"));
        }

        [Test]
        public void UnwantPlant_IncludesKind()
        {
            var json = new UnwantPlant(new SpeciesId(1)).Serialize();
            Assert.That(json, Does.Contain("unwant_plant"));
        }

        [Test]
        public void HasPlant_IncludesKind()
        {
            var json = new HasPlant(new SpeciesId(1), PlantOfferType.Seed, null).Serialize();
            Assert.That(json, Does.Contain("has_plant"));
        }
    }
}
