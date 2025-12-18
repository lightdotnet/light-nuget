namespace Light.Mail
{
    public class MailFrom
    {
        public MailFrom(string address)
        {
            Address = address;
        }

        public MailFrom(string address, string? displayName)
        {
            Address = address;
            DisplayName = displayName;
        }

        public string Address { get; set; } = null!;

        public string? DisplayName { get; set; }
    }
}
