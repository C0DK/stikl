using NUnit.Framework;
using Stikl.Web.Model;

namespace Stikl.Tests;

[TestFixture]
public class WantPlantTests
{
    static User BaseUser() => UserFactory.Create(Username.Parse("alice"));

    [Test]
    public void Apply_AddsSpeciesIdToWants()
    {
        var user = BaseUser();
        var speciesId = new SpeciesId(10);
        var payload = new WantPlant(speciesId);

        var updated = payload.Apply(user);

        Assert.That(updated.Wants, Contains.Item(speciesId));
    }

    [Test]
    public void Apply_DoesNotAffectOtherFields()
    {
        var user = BaseUser();
        var payload = new WantPlant(new SpeciesId(1));

        var updated = payload.Apply(user);

        Assert.That(updated.UserName, Is.EqualTo(user.UserName));
        Assert.That(updated.Email, Is.EqualTo(user.Email));
        Assert.That(updated.Has, Is.EqualTo(user.Has));
    }

    [Test]
    public void Apply_MultipleTimes_AccumulatesWants()
    {
        var user = BaseUser();
        var id1 = new SpeciesId(1);
        var id2 = new SpeciesId(2);

        var updated = new WantPlant(id1).Apply(user);
        updated = new WantPlant(id2).Apply(updated);

        Assert.That(updated.Wants, Contains.Item(id1));
        Assert.That(updated.Wants, Contains.Item(id2));
        Assert.That(updated.Wants, Has.Count.EqualTo(2));
    }

    [Test]
    public void Apply_DuplicateSpeciesId_DoesNotDuplicate()
    {
        var user = BaseUser();
        var id = new SpeciesId(5);

        var updated = new WantPlant(id).Apply(user);
        updated = new WantPlant(id).Apply(updated);

        Assert.That(updated.Wants, Has.Count.EqualTo(1));
    }

    [Test]
    public void EventKind_IsWantPlant()
    {
        var payload = new WantPlant(new SpeciesId(1));
        Assert.That(payload.EventKind, Is.EqualTo("want_plant"));
    }
}
