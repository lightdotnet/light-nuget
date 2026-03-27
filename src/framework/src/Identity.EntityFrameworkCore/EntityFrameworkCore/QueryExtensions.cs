namespace Light.Identity.EntityFrameworkCore;

public static class QueryExtensions
{
    public static Task<bool> CheckUserHasClaimAsync(this IdentityContext context, string userId, string claimType, string claimValue)
    {
        return context.UserClaims
            .Where(c => c.UserId == userId)
            .Select(c => new { c.ClaimType, c.ClaimValue })

            .Concat(
                from ur in context.UserRoles
                where ur.UserId == userId
                join rc in context.RoleClaims
                    on ur.RoleId equals rc.RoleId
                select new { rc.ClaimType, rc.ClaimValue }
            )

            .AnyAsync(c =>
                c.ClaimType == claimType &&
                c.ClaimValue == claimValue);
    }

    public static IQueryable<RoleClaim> QueryUserRoleClaims(this IdentityContext context, string userId)
    {
        return
            from ur in context.UserRoles
            where ur.UserId == userId
            join rc in context.RoleClaims
                on ur.RoleId equals rc.RoleId
            select rc;
    }
}
