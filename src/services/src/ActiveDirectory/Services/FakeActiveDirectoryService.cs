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

    public async Task<DomainUserDto?> GetByUserNameAsync(string userName)
    {
        await Task.Delay(1);
        return default;
    }
}
