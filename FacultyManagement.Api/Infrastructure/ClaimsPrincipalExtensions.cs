using System.Security.Claims;
using FacultyManagement.Business;

namespace FacultyManagement.Api.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static Guid UserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var value) ? value : throw new BusinessException("Authenticated user identifier is invalid.", 401);
    }
}
