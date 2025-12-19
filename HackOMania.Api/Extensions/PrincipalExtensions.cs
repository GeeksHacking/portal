using System.Security.Claims;
using HackOMania.Api.Constants;

namespace HackOMania.Api.Extensions;

public static class PrincipalExtensions
{
    extension(ClaimsPrincipal principal)
    {
        public T GetUserId<T>()
            where T : IParsable<T>
        {
            var raw = principal.Claims.Single(c => c is { Type: CustomClaimTypes.UserId });

            return T.TryParse(raw.Value, null, out var v)
                ? v
                : throw new InvalidOperationException("Invalid user ID");
        }
    }
}
