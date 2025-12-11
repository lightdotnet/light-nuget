using Light.ActiveDirectory.Dtos;
using Light.ActiveDirectory.Interfaces;
using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;

namespace Light.ActiveDirectory.Services;

[SupportedOSPlatform("windows")]
public class ActiveDirectoryService(DomainOptions settings) : IActiveDirectoryService
{
    public bool IsConfigured() => !string.IsNullOrEmpty(settings.Name);

    public Task<bool> CheckPasswordSignInAsync(string userName, string password)
    {
        // Create a context that will allow you to connect to your Domain Controller
        using var adContext = new PrincipalContext(ContextType.Domain, settings.Name);

        // find a user
        UserPrincipal user = UserPrincipal.FindByIdentity(adContext, userName);

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

    public Task<DomainUserDto?> GetByUserNameAsync(string userName)
    {
        using var adContext = new PrincipalContext(ContextType.Domain, settings.Name);
        {
            var adUser = UserPrincipal.FindByIdentity(adContext, userName);

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
}
