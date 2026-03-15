using NUnit.Framework;
using Stikl.Web.Model;

namespace Stikl.Tests;

[TestFixture]
public class UnwantPlantTests
{
    static User BaseUser() => UserFactory.Create(Username.Parse("alice"));

    [Test]
    public void Apply_RemovesSpeciesIdFromWants()
    {
        var user = BaseUser();
        var id = new SpeciesId(10);
        user = new WantPlant(id).Apply(user);

        var updated = new UnwantPlant(id).Apply(user);

        Assert.That(updated.Wants, Does.Not.Contain(id));
    }

    [Test]
    public void Apply_NonexistentId_LeavesWantsUnchanged()
    {
        var user = BaseUser();
        var existingId = new SpeciesId(1);
        user = new WantPlant(existingId).Apply(user);

        var updated = new UnwantPlant(new SpeciesId(999)).Apply(user);

        Assert.That(updated.Wants, Contains.Item(existingId));
        Assert.That(updated.Wants, Has.Count.EqualTo(1));
    }

    [Test]
    public void Apply_OnEmptyWants_RemainsEmpty()
    {
        var user = BaseUser();
        var updated = new UnwantPlant(new SpeciesId(5)).Apply(user);
        Assert.That(updated.Wants, Is.Empty);
    }

    [Test]
    public void Apply_DoesNotAffectOtherFields()
    {
        var user = BaseUser();
        var id = new SpeciesId(1);
        user = new WantPlant(id).Apply(user);

        var updated = new UnwantPlant(id).Apply(user);

        Assert.That(updated.UserName, Is.EqualTo(user.UserName));
        Assert.That(updated.Email, Is.EqualTo(user.Email));
        Assert.That(updated.Has, Is.EqualTo(user.Has));
    }

    [Test]
    public void EventKind_IsUnwantPlant()
    {
        var payload = new UnwantPlant(new SpeciesId(1));
        Assert.That(payload.EventKind, Is.EqualTo("unwant_plant"));
    }
}
