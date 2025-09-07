using System.Security.Claims;
using MyBooks.Common.Dtos;

namespace MyBooks.Common.Helpers;

public static class JwtClaimsHelper
{
    public static JwtClaimsDto ToJwtClaimsDto(this ClaimsPrincipal user)
    {
        return new JwtClaimsDto
        {
            UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
            Email = user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            Role = user.FindFirst("role")?.Value ?? string.Empty,
            TenantId = int.Parse(user.FindFirst("TenantId")?.Value ?? "0"),
            AgeCategoryId = int.Parse(user.FindFirst("AgeCategoryId")?.Value ?? "0"),
            IsActive = bool.Parse(user.FindFirst("IsActive")?.Value ?? "false"),
            AcceptedAup = bool.Parse(user.FindFirst("AcceptedAup")?.Value ?? "false")
        };
    }
}