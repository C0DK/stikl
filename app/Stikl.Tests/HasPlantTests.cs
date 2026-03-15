using NUnit.Framework;
using Stikl.Web.Model;

namespace Stikl.Tests;

[TestFixture]
public class HasPlantTests
{
    static User BaseUser() => UserFactory.Create(Username.Parse("alice"));

    [Test]
    public void Apply_AddsPlantOfferToHas()
    {
        var user = BaseUser();
        var id = new SpeciesId(10);
        var payload = new HasPlant(id, PlantOfferType.Seed, "Nice rose");

        var updated = payload.Apply(user);

        Assert.That(updated.Has, Has.Count.EqualTo(1));
        var offer = updated.Has.Single();
        Assert.That(offer.Id, Is.EqualTo(id));
        Assert.That(offer.Type, Is.EqualTo(PlantOfferType.Seed));
        Assert.That(offer.Comment, Is.EqualTo("Nice rose"));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Apply_NullOrWhitespaceComment_StoresNull(string? comment)
    {
        var user = BaseUser();
        var payload = new HasPlant(new SpeciesId(1), PlantOfferType.Sapling, comment);

        var updated = payload.Apply(user);

        Assert.That(updated.Has.Single().Comment, Is.Null);
    }

    [Test]
    public void Apply_NontrivialComment_PreservesComment()
    {
        var user = BaseUser();
        var payload = new HasPlant(new SpeciesId(2), PlantOfferType.Seed, "Great condition");

        var updated = payload.Apply(user);

        Assert.That(updated.Has.Single().Comment, Is.EqualTo("Great condition"));
    }

    [Test]
    public void Apply_DoesNotAffectWants()
    {
        var user = BaseUser();
        user = new WantPlant(new SpeciesId(99)).Apply(user);

        var updated = new HasPlant(new SpeciesId(1), PlantOfferType.Seed, null).Apply(user);

        Assert.That(updated.Wants, Has.Count.EqualTo(1));
    }

    [Test]
    public void Apply_MultipleDifferentOffers_AccumulatesAll()
    {
        var user = BaseUser();

        user = new HasPlant(new SpeciesId(1), PlantOfferType.Seed, null).Apply(user);
        user = new HasPlant(new SpeciesId(2), PlantOfferType.Sapling, "Healthy").Apply(user);

        Assert.That(user.Has, Has.Count.EqualTo(2));
    }

    [Test]
    public void EventKind_IsHasPlant()
    {
        var payload = new HasPlant(new SpeciesId(1), PlantOfferType.Seed, null);
        Assert.That(payload.EventKind, Is.EqualTo("has_plant"));
    }
}
