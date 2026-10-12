using Light.ActiveDirectory.Dtos;
using Light.ActiveDirectory.Interfaces;
using Novell.Directory.Ldap;
using System.DirectoryServices;
using System.Runtime.Versioning;

namespace Light.ActiveDirectory.Services;

[SupportedOSPlatform("windows")]
public class LDAPService(LdapOptions settings) : IActiveDirectoryService
{
    public bool IsConfigured() => true;

    [SupportedOSPlatform("windows")]
    public async Task<bool> CheckPasswordSignInAsync(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }
        // create LDAP connection
        using var ldapConn = new LdapConnection() { SecureSocketLayer = settings.UseSsl };

        // create socket connect to server
        await ldapConn.ConnectAsync(settings.Address, settings.Port);

        // bind domain user with domain name (username@domain.com) & password
        try
        {
            await ldapConn.BindAsync(userName + "@" + settings.Name, password);
        }
        catch (LdapException ex) when (ex.ResultCode == LdapException.InvalidCredentials)
        {
            return false;
        }

        return true;
    }

    public bool ChangePassword(string userName, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return false;
        }

        var sPath = settings.Connection; // This is if your domain was my.domain.com
        using var de = new DirectoryEntry(sPath, settings.UserName, settings.Password, AuthenticationTypes.Secure);
        using var ds = new DirectorySearcher(de);
        string qry = string.Format("(&(objectCategory=person)(objectClass=user)(sAMAccountName={0}))", EscapeLdapFilterValue(userName));
        ds.Filter = qry;
        var sr = ds.FindOne();
        if (sr is null)
        {
            return false;
        }

        using DirectoryEntry user = sr.GetDirectoryEntry();
        user.Invoke("SetPassword", [newPassword]);
        user.CommitChanges();

        return true;
    }

    public Task<DomainUserDto?> GetByUserNameAsync(string userName)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Escapes a value for use inside an LDAP search filter (RFC 4515),
    /// so input such as <c>*</c> or <c>)(</c> cannot alter the filter.
    /// </summary>
    internal static string EscapeLdapFilterValue(string value)
    {
        var sb = new System.Text.StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append(@"\5c"); break;
                case '*': sb.Append(@"\2a"); break;
                case '(': sb.Append(@"\28"); break;
                case ')': sb.Append(@"\29"); break;
                case '\0': sb.Append(@"\00"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}