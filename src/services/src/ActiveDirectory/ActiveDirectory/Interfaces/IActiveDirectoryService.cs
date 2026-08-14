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
        /// Administrative password reset for a user in Active Directory. Synchronous — despite
        /// other members here being async, this does not perform I/O asynchronously.
        /// </summary>
        bool ChangePassword(string userName, string newPassword);

        /// <summary>
        /// Get User Infomation from Active Directory.
        /// </summary>
        /// <remarks>
        /// Not implemented by <see cref="Services.LDAPService"/> — calling it there throws <see cref="NotImplementedException"/>.
        /// </remarks>
        Task<DomainUserDto?> GetByUserNameAsync(string userName);
    }
}