using Light.ActiveDirectory.Dtos;
using Light.ActiveDirectory.Interfaces;
using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;

namespace Light.ActiveDirectory.Services;

/// <summary>
/// <see cref="IActiveDirectoryService"/> backed by <c>System.DirectoryServices.AccountManagement</c>
/// for classic Windows domain-joined Active Directory.
/// </summary>
/// <remarks>
/// <c>System.DirectoryServices.AccountManagement</c> has no asynchronous API, so every member —
/// including the <c>...Async</c> ones — performs <b>blocking</b> network I/O against the domain
/// controller on the calling thread and returns an already-completed <see cref="Task"/>.
/// Each call also opens its own <see cref="PrincipalContext"/> (no connection reuse).
/// Offload to a background thread (e.g. <c>Task.Run</c>) if blocking the caller is a concern.
/// </remarks>
[SupportedOSPlatform("windows")]
public class ActiveDirectoryService(DomainOptions settings) : IActiveDirectoryService
{
    /// <summary>
    /// <see langword="true"/> when <see cref="DomainOptions.Name"/> is set to a real domain —
    /// i.e. it is not empty/whitespace and not the <c>"domain.com"</c> placeholder default.
    /// </summary>
    public bool IsConfigured() => DomainOptions.IsRealDomainName(settings.Name);

    /// <summary>
    /// Validates <paramref name="userName"/>/<paramref name="password"/> against the domain.
    /// Returns <see langword="false"/> if the user does not exist or is locked out.
    /// </summary>
    /// <remarks>Blocking: performs synchronous directory I/O and returns a completed task.</remarks>
    public Task<bool> CheckPasswordSignInAsync(string userName, string password)
    {
        // Create a context that will allow you to connect to your Domain Controller
        using var adContext = new PrincipalContext(ContextType.Domain, settings.Name);

        // find a user
        using var user = UserPrincipal.FindByIdentity(adContext, userName);

        //Check user is blocked
        if (user is not null && !user.IsAccountLockedOut())
        {
            var validate = adContext.ValidateCredentials(userName, password);
            if (validate)
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// <b>Administrative password reset</b> (not a user-initiated change): sets
    /// <paramref name="newPassword"/> via <see cref="AuthenticablePrincipal.SetPassword(string)"/>
    /// without verifying the current password. Requires the process identity to hold
    /// reset-password rights on the target account. Returns <see langword="false"/> if the user is not found.
    /// </summary>
    public bool ChangePassword(string userName, string newPassword)
    {
        using var adContext = new PrincipalContext(ContextType.Domain, settings.Name);
        using var user = UserPrincipal.FindByIdentity(adContext, userName);

        if (user is null)
        {
            return false;
        }

        user.SetPassword(newPassword);
        user.Save();

        return true;
    }

    /// <summary>
    /// Looks up a user by identity and maps it to a <see cref="DomainUserDto"/>, or <see langword="null"/> if not found.
    /// </summary>
    /// <remarks>Blocking: performs synchronous directory I/O and returns a completed task.</remarks>
    public Task<DomainUserDto?> GetByUserNameAsync(string userName)
    {
        using var adContext = new PrincipalContext(ContextType.Domain, settings.Name);
        using var adUser = UserPrincipal.FindByIdentity(adContext, userName);

        if (adUser != null)
        {
            var result = new DomainUserDto(adUser.UserPrincipalName)
            {
                FirstName = adUser.GivenName,
                LastName = adUser.Surname,
                PhoneNumber = adUser.VoiceTelephoneNumber,
                Email = adUser.EmailAddress,
            };

            return Task.FromResult<DomainUserDto?>(result);
        }

        return Task.FromResult<DomainUserDto?>(default);
    }
}
