using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stikl.Web.Model;

// TODO: we might want it to not be a message but an event that could also include "read" or other abstract things.
public record ChatEvent(
    int Pk,
    Username Sender,
    Username Recipient,
    DateTimeOffset Timestamp,
    ChatEventPayload Payload
);

public record Message(string Content) : ChatEventPayload
{
    [JsonIgnore]
    public const string Kind = "message";

    public override string EventKind => Kind;
}

public record Read : ChatEventPayload
{
    [JsonIgnore]
    public const string Kind = "read";

    public override string EventKind => Kind;
}

[JsonDerivedType(typeof(Read), Read.Kind)]
[JsonDerivedType(typeof(Message), Message.Kind)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "Kind")]
public abstract record ChatEventPayload
{
    [JsonIgnore]
    public abstract string EventKind { get; }

    public string Serialize() => JsonSerializer.Serialize(this);

    public static ChatEventPayload Deserialize(string payload) =>
        JsonSerializer.Deserialize<ChatEventPayload>(payload)!;
}
