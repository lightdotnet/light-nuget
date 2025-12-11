namespace Light.SmtpMail
{
    public abstract class SmtpConnection
    {
        public string Host { get; protected set; } = null!;

        public int Port { get; protected set; }

        public bool UseSsl { get; set; }
    }
}
