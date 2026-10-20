using System.Security.Claims;

namespace JajkoBackend.Helpers;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new InvalidOperationException("No user id claim"));
}
// usage: var userId = User.GetUserId();