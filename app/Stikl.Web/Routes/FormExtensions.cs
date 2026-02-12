namespace Stikl.Web.Routes;

public static class FormExtensions
{
    public static string? GetString(this IFormCollection form, string key)
    {
        if (form.TryGetValue(key, out var value))
            return value.SingleOrDefault();

        return null;
    }

    public static int? GetInt(this IFormCollection form, string key)
    {
        if (
            form.TryGetValue(key, out var stringValue)
            && int.TryParse(stringValue.SingleOrDefault(), out var value)
        )
            return value;

        return null;
    }
}
