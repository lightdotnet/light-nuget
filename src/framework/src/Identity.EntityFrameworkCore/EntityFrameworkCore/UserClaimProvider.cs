using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Light.Identity.EntityFrameworkCore;

public class UserClaimProvider(UserManager<User> userManager, RoleManager<Role> roleManager)
{
    public virtual async Task<IList<Claim>> GetUserClaimsAsync(User user)
    {
        //var userClaims = await userManager.GetClaimsAsync(user);
        var userRoles = await userManager.GetRolesAsync(user);

        //var roleClaims = new List<Claim>();
        var permissionClaims = new List<Claim>();

        foreach (var userRole in userRoles)
        {
            //roleClaims.Add(new Claim(ClaimTypes.Role, userRole));

            var role = await roleManager.FindByNameAsync(userRole);
            if (role is null)
                continue;

            var roleClaims = await roleManager.GetClaimsAsync(role);

            permissionClaims.AddRange(roleClaims);
        }

        var claims = new List<Claim>
        {
            { ClaimTypes.UserId, user.Id },
            { ClaimTypes.UserName, user.UserName },
            //{ ClaimTypes.FirstName, user.FirstName },
            //{ ClaimTypes.LastName, user.LastName },
            //{ ClaimTypes.PhoneNumber, user.PhoneNumber },
            //{ ClaimTypes.Email, user.Email },
        }
        //.Union(userClaims)
        //.Union(roleClaims)
        .Union(permissionClaims)
        .Where(x => !string.IsNullOrEmpty(x.Value))
        .ToList();

        return claims;
    }
}
