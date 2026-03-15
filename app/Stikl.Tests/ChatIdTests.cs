using NUnit.Framework;
using Stikl.Web.Model;

namespace Stikl.Tests;

[TestFixture]
public class ChatIdTests
{
    [TestCase("1", 1)]
    [TestCase("100", 100)]
    [TestCase("0", 0)]
    public void TryParse_ValidInteger_ReturnsTrue(string input, int expected)
    {
        var result = ChatId.TryParse(input, out var id);
        Assert.That(result, Is.True);
        Assert.That(id.Value, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("abc")]
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        var result = ChatId.TryParse(input, out var id);
        Assert.That(result, Is.False);
        Assert.That(id, Is.EqualTo(default(ChatId)));
    }

    [Test]
    public void Parse_ValidString_ReturnsChatId()
    {
        var id = ChatId.Parse("5");
        Assert.That(id.Value, Is.EqualTo(5));
    }

    [Test]
    public void Parse_InvalidString_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ChatId.Parse("bad"));
    }

    [Test]
    public void ToString_ReturnsIntegerString()
    {
        var id = new ChatId(42);
        Assert.That(id.ToString(), Is.EqualTo("42"));
    }

    [Test]
    public void ImplicitIntConversion_ReturnsValue()
    {
        var id = new ChatId(3);
        int value = id;
        Assert.That(value, Is.EqualTo(3));
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = new ChatId(7);
        var b = new ChatId(7);
        Assert.That(a.Equals(b), Is.True);
    }

    [Test]
    public void GetHashCode_SameValue_SameHash()
    {
        var a = new ChatId(99);
        var b = new ChatId(99);
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
    }

    [Test]
    public void Equals_Null_ReturnsFalse()
    {
        var id = new ChatId(1);
        Assert.That(id.Equals((ChatId?)null), Is.False);
    }
}
