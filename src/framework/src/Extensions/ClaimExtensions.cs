using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Light.Extensions
{
    public static class ClaimExtensions
    {
        public static List<Claim> Add(this List<Claim> claims, string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                claims.Add(new Claim(key, value));
            }

            return claims;
        }

        public static string? FindFirstValue(this ClaimsPrincipal principal, string claimType) =>
            principal is null
                ? throw new ArgumentNullException(nameof(principal))
                : principal.FindFirst(claimType)?.Value;
    }
}
