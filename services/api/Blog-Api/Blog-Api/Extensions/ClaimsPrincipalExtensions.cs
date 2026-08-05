using System.Security.Claims;

namespace BlogApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        if (principal.Identity is null)
        {
            return Guid.Empty;
        }

        string? idString = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (idString is null)
        {
            throw new InvalidOperationException("Principal NameIdentifier is null");
        }

        return Guid.Parse(idString);
    }
}