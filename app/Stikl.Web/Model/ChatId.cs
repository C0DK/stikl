using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Strongbars.Abstractions;

namespace Stikl.Web.Model;

[JsonConverter(typeof(ChatId.DefaultJsonConverter))]
public readonly record struct ChatId(int Value) : IEquatable<ChatId?>
{
    public override string ToString() => Value.ToString();

    public static ChatId Parse(string value)
    {
        if (TryParse(value, out var output))
            return output;

        throw new InvalidOperationException($"Plant id '{value}' is not valid!");
    }

    public static bool TryParse(string? value, [NotNullWhen(true)] out ChatId output)
    {
        if (value is null || !int.TryParse(value, out var intValue))
        {
            output = default;
            return false;
        }
        output = new ChatId(intValue);
        return true;
    }

    public bool Equals(ChatId? other)
    {
        if (other is null)
            return false;
        return Value == other.Value;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + Value.GetHashCode();
            return hash;
        }
    }

    public static implicit operator int(ChatId id) => id.Value;

    public static implicit operator TemplateArgument(ChatId value) => value.ToString();

    public class DefaultJsonConverter : JsonConverter<ChatId>
    {
        public override ChatId Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return new ChatId(reader.GetInt32());
            }
            throw new JsonException($"Expected number, found {reader.TokenType}");
        }

        public override void Write(
            Utf8JsonWriter writer,
            ChatId value,
            JsonSerializerOptions options
        )
        {
            writer.WriteNumberValue(value.Value);
        }
    }
}
