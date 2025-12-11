using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Light.Identity.EntityFrameworkCore;

public class UserService(UserManager<User> userManager) : IUserService
{
    protected UserManager<User> UserManager => userManager;

    public virtual async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        return await userManager.Users
            .AsNoTracking()
            .OrderByDescending(x => x.Created)
            .ThenBy(x => x.UserName)
            .MapToDto()
            .ToListAsync();
    }

    private async Task<UserDto> GetByAsync(User user)
    {
        var dto = user.MapToDto();

        dto.Roles = await userManager.GetRolesAsync(user);

        var userClaims = await userManager.GetClaimsAsync(user);

        dto.Claims = [..userClaims.Select(s => new ClaimDto
        {
            Type = s.Type,
            Value = s.Value
        })];

        return dto;
    }

    private async Task UpdateRolesAsync(User user, IEnumerable<string> roles)
    {
        var userRoles = await userManager.GetRolesAsync(user);

        var removeRoles = userRoles.Except(roles);
        if (removeRoles.Any())
        {
            await userManager.RemoveFromRolesAsync(user, removeRoles);
        }

        var assignRoles = roles.Except(userRoles);
        if (assignRoles.Any())
        {
            await userManager.AddToRolesAsync(user, assignRoles);
        }
    }

    private async Task UpdateClaimsAsync(User user, IEnumerable<Claim> claims)
    {
        var userClaims = await userManager.GetClaimsAsync(user);

        var removeClaims = userClaims.Except(claims);
        if (removeClaims.Any())
        {
            await userManager.RemoveClaimsAsync(user, removeClaims);
        }

        var assignClaims = claims.Except(userClaims);
        if (assignClaims.Any())
        {
            await userManager.AddClaimsAsync(user, assignClaims);
        }
    }

    public virtual async Task<IResult<UserDto>> GetByIdAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);

        if (user == null)
            return Result<UserDto>.NotFound($"User {id} not found");

        return Result<UserDto>.Success(await GetByAsync(user));
    }

    public virtual async Task<IResult<UserDto>> GetByUserNameAsync(string userName)
    {
        var user = await userManager.FindByNameAsync(userName);

        if (user == null)
            return Result<UserDto>.NotFound($"User {userName} not found");

        return Result<UserDto>.Success(await GetByAsync(user));
    }

    public virtual async Task<IResult<string>> CreateAsync(CreateUserRequest newUser)
    {
        var entity = new User
        {
            UserName = newUser.UserName,
            Email = newUser.Email,
            PhoneNumber = newUser.PhoneNumber,
            FirstName = newUser.FirstName,
            LastName = newUser.LastName,
            AuthProvider = newUser.AuthProvider,
        };

        var identityResult = !string.IsNullOrEmpty(newUser.Password)
            ? await userManager.CreateAsync(entity, newUser.Password)
            : await userManager.CreateAsync(entity);

        return identityResult.ToResult(entity.Id);
    }

    public virtual async Task<IResult> UpdateAsync(UserDto updateUser)
    {
        var user = await userManager.FindByIdAsync(updateUser.Id);

        if (user == null)
            return Result.NotFound($"User {updateUser.Id} not found");

        // update base info
        user.UpdateInfo(
            updateUser.FirstName,
            updateUser.LastName,
            updateUser.PhoneNumber,
            updateUser.Email);

        // update status
        user.UpdateStatus(updateUser.Status);

        // update auth provider
        user.ChangeAuthProvider(updateUser.AuthProvider);

        var updatedResult = await userManager.UpdateAsync(user);
        if (!updatedResult.Succeeded)
            return updatedResult.ToResult();

        await UpdateRolesAsync(user, updateUser.Roles);

        await UpdateClaimsAsync(user, updateUser.Claims
            .Distinct()
            .Select(c => new Claim(c.Type, c.Value)));

        return Result.Success();
    }

    public virtual async Task<IResult> DeleteAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);

        if (user == null)
            return Result.NotFound($"User {id} not found");

        user.Delete();

        var identityResult = await userManager.DeleteAsync(user);

        return identityResult.ToResult();
    }

    public virtual async Task<IResult> ForcePasswordAsync(string id, string password)
    {
        var user = await userManager.FindByIdAsync(id);

        if (user == null)
            return Result.NotFound($"User {id} not found");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        var identityResult = await userManager.ResetPasswordAsync(user, token, password);

        return identityResult.ToResult();
    }

    public virtual async Task<IEnumerable<UserDto>> GetUsersHasClaimAsync(string claimType, string claimValue)
    {
        var users = await userManager.GetUsersForClaimAsync(new Claim(claimType, claimValue));

        return users.Select(s => s.MapToDto());
    }
}
