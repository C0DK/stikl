using System.Text.Json.Serialization;

namespace Stikl.Web.Model;

[JsonDerivedType(typeof(UserCreated))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
public abstract record UserEventPayload
{
    public abstract User Apply(User user);
}
