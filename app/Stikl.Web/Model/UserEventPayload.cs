using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stikl.Web.Model;

[JsonDerivedType(typeof(UserCreated), UserCreated.Kind)]
[JsonDerivedType(typeof(WantPlant), WantPlant.Kind)]
[JsonDerivedType(typeof(UnwantPlant), UnwantPlant.Kind)]
[JsonDerivedType(typeof(HasPlant), HasPlant.Kind)]
[JsonDerivedType(typeof(NoLongerHasPlant), NoLongerHasPlant.Kind)]
[JsonDerivedType(typeof(UpdateName), UpdateName.Kind)]
[JsonDerivedType(typeof(UpdateLocation), UpdateLocation.Kind)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "Kind")]
public abstract record UserEventPayload
{
    [JsonIgnore]
    public abstract string EventKind { get; }

    public abstract User Apply(User user);

    // TODO: better serializer of various stuff i.e email
    // TODO: default options
    public string Serialize() => JsonSerializer.Serialize(this);

    public static UserEventPayload Deserialize(string payload) =>
        JsonSerializer.Deserialize<UserEventPayload>(payload)!;
}
