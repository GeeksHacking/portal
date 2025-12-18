using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace HackOMania.Api.Extensions;

public static class PrincipalExtensions
{
    extension(ClaimsPrincipal principal)
    {
        public Guid? GetUserId()
        {
            var claim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (claim is null)
            {
                return null;
            }

            return Guid.TryParse(claim.Value, out var id) ? id : null;
        }

        public T GetUserId<T>()
            where T : IParsable<T>
        {
            var raw = principal.Claims.Single(c =>
                c.Type == ClaimTypes.NameIdentifier
                && c.Issuer == CookieAuthenticationDefaults.AuthenticationScheme
            );

            return T.Parse(raw.Value, null);
        }
    }
}
