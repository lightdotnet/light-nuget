using Light.ActiveDirectory.Dtos;
using Light.ActiveDirectory.Interfaces;

namespace Light.ActiveDirectory.Services;

public class FakeActiveDirectoryService : IActiveDirectoryService
{
    public bool IsConfigured() => false;

    public Task<bool> CheckPasswordSignInAsync(string userName, string password)
    {
        return Task.FromResult(false);
    }

    public bool ChangePassword(string userName, string newPassword) => false;

    public Task<DomainUserDto?> GetByUserNameAsync(string userName)
    {
        return Task.FromResult<DomainUserDto?>(default);
    }
}
