using System.Diagnostics.CodeAnalysis;
using Strongbars.Abstractions;

namespace Stikl.Web.Model;

// TODO: remove constructor so we always lower it?
public readonly record struct Email(string Value)
{
    public override string ToString() => Value;

    public static Email Parse(string value)
    {
        if (TryParse(value, out var email))
            return email;

        throw new InvalidOperationException($"Email '{value}' is not valid!");
    }

    public static bool TryParse(string? value, [NotNullWhen(true)] out Email output)
    {
        if (value is not { Length: > 0 } || !IsValidEmail(value))
        {
            output = default;
            return false;
        }
        output = new(value.ToLowerInvariant());
        return true;
    }

    public static implicit operator string(Email email) => email.ToString();

    public static implicit operator TemplateArgument(Email value) => value.ToString();

    static bool IsValidEmail(string email)
    {
        var trimmedEmail = email.Trim();

        if (trimmedEmail.EndsWith("."))
        {
            return false; // suggested by @TK-421
        }
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == trimmedEmail;
        }
        catch
        {
            return false;
        }
    }
}
