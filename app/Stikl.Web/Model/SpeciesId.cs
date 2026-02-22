using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Strongbars.Abstractions;

namespace Stikl.Web.Model;

[JsonConverter(typeof(SpeciesId.DefaultJsonConverter))]
public readonly record struct SpeciesId(int Value) : IEquatable<SpeciesId?>
{
    public override string ToString() => Value.ToString();

    public static SpeciesId Parse(string value)
    {
        if (TryParse(value, out var output))
            return output;

        throw new InvalidOperationException($"Plant id '{value}' is not valid!");
    }

    public static bool TryParse(string? value, [NotNullWhen(true)] out SpeciesId output)
    {
        if (value is null || !int.TryParse(value, out var intValue))
        {
            output = default;
            return false;
        }
        output = new SpeciesId(intValue);
        return true;
    }

    public bool Equals(SpeciesId? other)
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

    public static implicit operator int(SpeciesId id) => id.Value;

    public static implicit operator TemplateArgument(SpeciesId value) => value.ToString();

    public class DefaultJsonConverter : JsonConverter<SpeciesId>
    {
        public override SpeciesId Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return new SpeciesId(reader.GetInt32());
            }
            throw new JsonException($"Expected number, found {reader.TokenType}");
        }

        public override void Write(
            Utf8JsonWriter writer,
            SpeciesId value,
            JsonSerializerOptions options
        )
        {
            writer.WriteNumberValue(value.Value);
        }
    }
}
