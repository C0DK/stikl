using NUnit.Framework;
using Stikl.Web.Model;

namespace Stikl.Tests;

[TestFixture]
public class SpeciesIdTests
{
    [TestCase("1", 1)]
    [TestCase("42", 42)]
    [TestCase("0", 0)]
    [TestCase("-5", -5)]
    public void TryParse_ValidInteger_ReturnsTrue(string input, int expected)
    {
        var result = SpeciesId.TryParse(input, out var id);
        Assert.That(result, Is.True);
        Assert.That(id.Value, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("abc")]
    [TestCase("1.5")]
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        var result = SpeciesId.TryParse(input, out var id);
        Assert.That(result, Is.False);
        Assert.That(id, Is.EqualTo(default(SpeciesId)));
    }

    [Test]
    public void Parse_ValidString_ReturnsSpeciesId()
    {
        var id = SpeciesId.Parse("42");
        Assert.That(id.Value, Is.EqualTo(42));
    }

    [Test]
    public void Parse_InvalidString_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SpeciesId.Parse("notanumber"));
    }

    [Test]
    public void ToString_ReturnsIntegerString()
    {
        var id = new SpeciesId(99);
        Assert.That(id.ToString(), Is.EqualTo("99"));
    }

    [Test]
    public void ImplicitIntConversion_ReturnsValue()
    {
        var id = new SpeciesId(7);
        int value = id;
        Assert.That(value, Is.EqualTo(7));
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = new SpeciesId(10);
        var b = new SpeciesId(10);
        Assert.That(a, Is.EqualTo(b));
        Assert.That(a.Equals(b), Is.True);
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new SpeciesId(10);
        var b = new SpeciesId(20);
        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void GetHashCode_SameValue_SameHash()
    {
        var a = new SpeciesId(42);
        var b = new SpeciesId(42);
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
    }

    [Test]
    public void Equals_Null_ReturnsFalse()
    {
        var id = new SpeciesId(1);
        Assert.That(id.Equals((SpeciesId?)null), Is.False);
    }
}
