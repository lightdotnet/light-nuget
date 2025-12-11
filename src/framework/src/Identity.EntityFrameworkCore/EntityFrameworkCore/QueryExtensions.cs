namespace Light.Identity.EntityFrameworkCore;

public static class QueryExtensions
{
    public static async Task<bool> CheckUserHasClaimAsync(this IdentityContext context, string userId, string claimType, string claimValue)
    {
        var hasUserClaim = await context.UserClaims
            .AnyAsync(x =>
                x.UserId == userId
                && x.ClaimType == claimType
                && x.ClaimValue == claimValue);

        if (hasUserClaim)
        {
            return hasUserClaim;
        }

        var hasRoleClaim = await (
            from user_roles in context.UserRoles.Where(x => x.UserId == userId)
            join roles in context.Roles
                on user_roles.RoleId equals roles.Id
            join role_Claims in context.RoleClaims.Where(x => x.ClaimType == claimType && x.ClaimValue == claimValue)
                on roles.Id equals role_Claims.RoleId
            select new
            {
                user_roles.UserId,
            })
            .AnyAsync();

        return hasRoleClaim;
    }

    public static IQueryable<RoleClaim> GetUserRoleClaimsAsync(this IdentityContext context, string userId)
    {
        return
            from user_roles in context.UserRoles.Where(x => x.UserId == userId)
            join roles in context.Roles
                on user_roles.RoleId equals roles.Id
            join role_Claims in context.RoleClaims
                on roles.Id equals role_Claims.RoleId
            select role_Claims;
    }
}
