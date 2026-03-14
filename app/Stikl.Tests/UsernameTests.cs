using NUnit.Framework;
using Stikl.Web.Model;

namespace Stikl.Tests;

[TestFixture]
public class UsernameTests
{
    [TestCase("alice")]
    [TestCase("Alice")]
    [TestCase("a1b2c3")]
    [TestCase("user_name")]
    [TestCase("user123")]
    [TestCase("a")]
    public void TryParse_ValidUsername_ReturnsTrue(string input)
    {
        var result = Username.TryParse(input, out var username);
        Assert.That(result, Is.True);
        Assert.That(username.Value, Is.EqualTo(input.ToLowerInvariant()));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("1startswithdigit")]
    [TestCase("_startswithunderscore")]
    [TestCase("has space")]
    [TestCase("has-hyphen")]
    [TestCase("has@symbol")]
    public void TryParse_InvalidUsername_ReturnsFalse(string? input)
    {
        var result = Username.TryParse(input, out var username);
        Assert.That(result, Is.False);
        Assert.That(username, Is.EqualTo(default(Username)));
    }

    [Test]
    public void TryParse_NormalizesToLowercase()
    {
        Username.TryParse("Alice123", out var username);
        Assert.That(username.Value, Is.EqualTo("alice123"));
    }

    [Test]
    public void Parse_ValidUsername_ReturnsUsername()
    {
        var username = Username.Parse("alice");
        Assert.That(username.Value, Is.EqualTo("alice"));
    }

    [Test]
    public void Parse_InvalidUsername_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Username.Parse("123invalid"));
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        var username = Username.Parse("alice");
        Assert.That(username.ToString(), Is.EqualTo("alice"));
    }

    [Test]
    public void ImplicitStringConversion_ReturnsValue()
    {
        var username = Username.Parse("alice");
        string s = username;
        Assert.That(s, Is.EqualTo("alice"));
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = Username.Parse("alice");
        var b = Username.Parse("alice");
        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void Equality_DifferentCasing_AreEqualAfterNormalization()
    {
        var a = Username.Parse("Alice");
        var b = Username.Parse("alice");
        Assert.That(a, Is.EqualTo(b));
    }
}
