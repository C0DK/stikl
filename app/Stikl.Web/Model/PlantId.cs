using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Strongbars.Abstractions;

namespace Stikl.Web.Model;

public readonly record struct PlantId(string Value) : IEquatable<PlantId?>
{
    public override string ToString() => Value;

    public static PlantId Parse(string value)
    {
        if (TryParse(value, out var output))
            return output;

        throw new InvalidOperationException($"Plant id '{value}' is not valid!");
    }

    public static bool TryParse(string? value, [NotNullWhen(true)] out PlantId output)
    {
        // Regex compiled..
        if (value is null || !Regex.IsMatch(value, @"^[a-zA-Z][\w\d_]*$"))
        {
            output = default;
            return false;
        }
        output = new PlantId(value.ToLowerInvariant());
        return true;
    }

    public bool Equals(PlantId? other)
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

    public static implicit operator string(PlantId value) => value.ToString();

    public static implicit operator TemplateArgument(PlantId value) => value.ToString();
}
