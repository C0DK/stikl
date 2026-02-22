using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Strongbars.Abstractions;

namespace Stikl.Web.Model;

[JsonConverter(typeof(Username.DefaultJsonConverter))]
public readonly record struct Username(string Value)
{
    public override string ToString() => Value;

    public static Username Parse(string value)
    {
        if (TryParse(value, out var output))
            return output;

        throw new InvalidOperationException($"Username '{value}' is not valid!");
    }

    public static bool TryParse(string? value, [NotNullWhen(true)] out Username output)
    {
        // Regex compiled..
        if (value is null || !Regex.IsMatch(value, @"^[a-zA-Z][\w\d_]*$"))
        {
            output = default;
            return false;
        }
        output = new Username(value.ToLowerInvariant());
        return true;
    }

    public static implicit operator string(Username value) => value.ToString();

    public static implicit operator TemplateArgument(Username value) => value.ToString();

    public class DefaultJsonConverter : JsonConverter<Username>
    {
        public override Username Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (
                reader.TokenType == JsonTokenType.String
                && reader.GetString() is { } value
                && Username.TryParse(value, out var email)
            )
            {
                return email;
            }
            throw new JsonException($"Expected string, found {reader.TokenType}");
        }

        public override void Write(
            Utf8JsonWriter writer,
            Username value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
