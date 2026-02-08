using System.Security.Claims;
using Stikl.Web.Model;

namespace Stikl.Web.Routes;

public static class ClaimsExtensions
{
    public static Email? GetEmailOrNull(this ClaimsPrincipal principal)
    {
        var claim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

        if (claim is null)
            return null;

        return Email.Parse(claim.Value);
    }

    public static Email GetEmail(this ClaimsPrincipal principal) =>
        principal.GetEmailOrNull() ?? throw new NullReferenceException();

    public static Username? GetUsernameOrNull(this ClaimsPrincipal principal)
    {
        var claim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

        if (claim is null)
            return null;

        return Username.Parse(claim.Value);
    }

    public static Username GetUsername(this ClaimsPrincipal principal) =>
        principal.GetUsernameOrNull() ?? throw new NullReferenceException();

    public static string? GetFirstNameOrNull(this ClaimsPrincipal principal)
    {
        var claim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName);

        if (claim is null)
            return null;

        return claim.Value; // TODO: capitalize first
    }

    public static string GetFirstName(this ClaimsPrincipal principal) =>
        principal.GetFirstNameOrNull() ?? throw new NullReferenceException();
}
