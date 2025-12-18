namespace Light.ActiveDirectory
{
    public class LdapOptions
    {
        public string Name { get; set; } = "domain.com";

        public string Address { get; set; } = "10.0.10.2";

        public int Port { get; set; } = 389;

        public string Connection { get; set; } = "LDAP://127.0.0.1/DC=company,DC=local";

        public string NewUserConnection { get; set; } = "LDAP://127.0.0.1/ou=new_users,DC=company,DC=local";

        public string UserName { get; set; } = "admin";

        public string Password { get; set; } = "AdminP@ssword";
    }
}