using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Light.Identity.EntityFrameworkCore;

public class RoleService(RoleManager<Role> roleManager) : IRoleService
{
    protected RoleManager<Role> RoleManager => roleManager;

    public virtual async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        return await roleManager.Roles
            .AsNoTracking()
            .MapToDto()
            .ToListAsync();
    }

    public virtual async Task<IResult<RoleDto>> GetByIdAsync(string id)
    {
        var role = await roleManager.FindByIdAsync(id);

        if (role == null)
            return Result<RoleDto>.NotFound($"Role {id} not found");

        var result = role.MapToDto();

        result.Claims = await GetRoleClaimsAsync(role);

        return Result<RoleDto>.Success(result);
    }

    public virtual async Task<IResult<RoleDto>> GetByNameAsync(string name)
    {
        var role = await roleManager.FindByNameAsync(name);

        if (role == null)
            return Result<RoleDto>.NotFound($"Role {name} not found");

        var result = role.MapToDto();

        result.Claims = await GetRoleClaimsAsync(role);

        return Result<RoleDto>.Success(result);
    }

    public virtual async Task<IList<ClaimDto>> GetRoleClaimsAsync(Role role)
    {
        var claims = await roleManager.GetClaimsAsync(role);

        return [.. claims.Select(s => new ClaimDto
            {
                Type = s.Type,
                Value = s.Value
            })];
    }

    public virtual async Task<IResult<string>> CreateAsync(CreateRoleRequest request)
    {
        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
        };

        var result = await roleManager.CreateAsync(role);

        return result.ToResult(role.Id);
    }

    public virtual async Task<IResult> UpdateAsync(RoleDto request)
    {
        var role = await roleManager.FindByIdAsync(request.Id);

        if (role == null)
            return Result.NotFound($"Role {request.Id} not found");

        role.Update(request.Name, request.Description);

        var result = await roleManager.UpdateAsync(role);

        if (result.Succeeded)
        {
            await UpdateClaimsAsync(role, request.Claims);
        }

        return result.ToResult();
    }

    public virtual async Task UpdateClaimsAsync(Role role, IEnumerable<ClaimDto> claims)
    {
        // get claims assigned to role
        var roleClaims = await roleManager.GetClaimsAsync(role);

        var requestClaims = claims.Select(s => new Claim(s.Type, s.Value));

        var roleClaimsToRemove = roleClaims.Except(requestClaims);

        // remove claims not in request list from role
        foreach (var claim in roleClaimsToRemove)
        {
            await roleManager.RemoveClaimAsync(role, claim);
        }

        var roleClaimsToAdd = requestClaims.Except(roleClaims);

        // assigned new claims in request list to role
        foreach (var claim in roleClaimsToAdd)
        {
            await roleManager.AddClaimAsync(role, claim);
        }
    }

    public virtual async Task<IResult> DeleteAsync(string id)
    {
        var role = await roleManager.FindByIdAsync(id);

        if (role == null)
            return Result.NotFound($"Role {id} not found");

        // Check claim exist for role
        var claimsByRole = await roleManager.GetClaimsAsync(role);

        if (claimsByRole.Any())
            return Result.Error("Role has already setup claims.");

        var result = await roleManager.DeleteAsync(role);

        return result.ToResult();
    }
}
