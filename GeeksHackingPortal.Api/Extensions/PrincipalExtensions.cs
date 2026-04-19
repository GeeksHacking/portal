using System.Security.Claims;
using GeeksHackingPortal.Api.Constants;

namespace GeeksHackingPortal.Api.Extensions;

public static class PrincipalExtensions
{
    extension(ClaimsPrincipal principal)
    {
        public Guid? GetUserId()
        {
            var raw = principal.Claims.SingleOrDefault(c => c is { Type: CustomClaimTypes.UserId });
            return Guid.TryParse(raw?.Value, out var v) ? v : null;
        }
    }
}
