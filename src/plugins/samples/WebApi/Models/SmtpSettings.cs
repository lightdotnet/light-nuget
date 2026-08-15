namespace WebApi.Models
{
    public class SmtpSettings
    {
        public string Host { get; set; } = null!;

        public int Port { get; set; }

        public bool UseSsl { get; set; }

        public string UserName { get; set; } = null!;

        public string Password { get; set; } = null!;
    }
}
