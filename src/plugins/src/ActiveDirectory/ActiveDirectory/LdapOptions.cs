namespace Light.ActiveDirectory
{
    public class LdapOptions
    {
        public string Name { get; set; } = "domain.com";

        public string Address { get; set; } = "10.0.10.2";

        public int Port { get; set; } = 389;

        /// <summary>
        /// Use LDAPS (SSL) for the credential-check bind. Default false for backward compatibility;
        /// strongly recommended in production (typically with <see cref="Port"/> = 636),
        /// otherwise user passwords are sent to the server in cleartext.
        /// </summary>
        public bool UseSsl { get; set; }

        public string Connection { get; set; } = "LDAP://127.0.0.1/DC=company,DC=local";

        public string NewUserConnection { get; set; } = "LDAP://127.0.0.1/ou=new_users,DC=company,DC=local";

        public string UserName { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}