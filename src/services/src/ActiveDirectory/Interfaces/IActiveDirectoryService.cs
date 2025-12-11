using Light.ActiveDirectory.Dtos;

namespace Light.ActiveDirectory.Interfaces
{
    public interface IActiveDirectoryService
    {
        /// <summary>
        /// Check AD information is configured
        /// </summary>
        bool IsConfigured();

        /// <summary>
        /// Check userName & password from Active Directory
        /// </summary>
        Task<bool> CheckPasswordSignInAsync(string userName, string password);

        /// <summary>
        /// Get User Infomation from Active Directory
        /// </summary>
        Task<DomainUserDto?> GetByUserNameAsync(string userName);
    }
}