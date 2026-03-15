using System.Reflection;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Stikl.Web.Model;

namespace Stikl.Tests;

/// <summary>
/// Ensures that every concrete subtype of UserEventPayload and ChatEventPayload is:
///   1. Registered via [JsonDerivedType] on the base class.
///   2. Has a public const string Kind whose value matches the discriminator.
///   3. Has a Kind value that is unique across all subtypes.
///
/// Adding a new event type without wiring it up will fail here immediately.
/// </summary>
[TestFixture]
public class EventPayloadRegistrationTests
{
    static IReadOnlyList<Type> ConcreteSubtypes(Type baseType) =>
        baseType
            .Assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsSubclassOf(baseType))
            .ToList();

    static string KindOf(Type t)
    {
        var field = t.GetField("Kind", BindingFlags.Public | BindingFlags.Static);
        Assert.That(
            field,
            Is.Not.Null,
            $"{t.Name} must have a public const string Kind"
        );
        return (string)field!.GetValue(null)!;
    }

    static Dictionary<Type, string> RegisteredDiscriminators(Type baseType) =>
        baseType
            .GetCustomAttributes<JsonDerivedTypeAttribute>()
            .ToDictionary(a => a.DerivedType, a => (string)a.TypeDiscriminator!);

    [TestFixture]
    public class UserEventPayloads
    {
        static IReadOnlyList<Type> Subtypes => ConcreteSubtypes(typeof(UserEventPayload));

        [Test]
        public void AllConcreteSubtypes_HaveKindConstant() =>
            Assert.Multiple(() =>
            {
                foreach (var t in Subtypes)
                    Assert.That(
                        t.GetField("Kind", BindingFlags.Public | BindingFlags.Static),
                        Is.Not.Null,
                        $"{t.Name} is missing public const string Kind"
                    );
            });

        [Test]
        public void KindValues_AreUnique()
        {
            var kinds = Subtypes.Select(KindOf).ToList();
            Assert.That(kinds, Is.Unique);
        }

        [Test]
        public void AllSubtypes_AreRegisteredWithJsonDerivedType() =>
            Assert.Multiple(() =>
            {
                var registered = RegisteredDiscriminators(typeof(UserEventPayload));
                foreach (var t in Subtypes)
                    Assert.That(
                        registered.ContainsKey(t),
                        Is.True,
                        $"{t.Name} is missing [JsonDerivedType] on UserEventPayload"
                    );
            });

        [Test]
        public void JsonDerivedTypeDiscriminator_MatchesKindConstant() =>
            Assert.Multiple(() =>
            {
                var registered = RegisteredDiscriminators(typeof(UserEventPayload));
                foreach (var t in Subtypes)
                {
                    if (!registered.TryGetValue(t, out var discriminator))
                        continue;
                    Assert.That(
                        discriminator,
                        Is.EqualTo(KindOf(t)),
                        $"{t.Name}: [JsonDerivedType] discriminator does not match Kind constant"
                    );
                }
            });
    }

    [TestFixture]
    public class ChatEventPayloads
    {
        static IReadOnlyList<Type> Subtypes => ConcreteSubtypes(typeof(ChatEventPayload));

        [Test]
        public void AllConcreteSubtypes_HaveKindConstant() =>
            Assert.Multiple(() =>
            {
                foreach (var t in Subtypes)
                    Assert.That(
                        t.GetField("Kind", BindingFlags.Public | BindingFlags.Static),
                        Is.Not.Null,
                        $"{t.Name} is missing public const string Kind"
                    );
            });

        [Test]
        public void KindValues_AreUnique()
        {
            var kinds = Subtypes.Select(KindOf).ToList();
            Assert.That(kinds, Is.Unique);
        }

        [Test]
        public void AllSubtypes_AreRegisteredWithJsonDerivedType() =>
            Assert.Multiple(() =>
            {
                var registered = RegisteredDiscriminators(typeof(ChatEventPayload));
                foreach (var t in Subtypes)
                    Assert.That(
                        registered.ContainsKey(t),
                        Is.True,
                        $"{t.Name} is missing [JsonDerivedType] on ChatEventPayload"
                    );
            });

        [Test]
        public void JsonDerivedTypeDiscriminator_MatchesKindConstant() =>
            Assert.Multiple(() =>
            {
                var registered = RegisteredDiscriminators(typeof(ChatEventPayload));
                foreach (var t in Subtypes)
                {
                    if (!registered.TryGetValue(t, out var discriminator))
                        continue;
                    Assert.That(
                        discriminator,
                        Is.EqualTo(KindOf(t)),
                        $"{t.Name}: [JsonDerivedType] discriminator does not match Kind constant"
                    );
                }
            });
    }
}
