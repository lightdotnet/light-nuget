namespace Light.SmtpMail
{
    public class SmtpMailOptions
    {
        public string Host { get; set; } = null!;

        public virtual int Port { get; set; } = 25;

        public bool UseSsl { get; set; }
    }

    public class SmtpMailKitOptions : SmtpMailOptions
    {
        public override int Port { get; set; } = 587;

        public string UserName { get; set; } = null!;

        public string Password { get; set; } = null!;
    }
}
