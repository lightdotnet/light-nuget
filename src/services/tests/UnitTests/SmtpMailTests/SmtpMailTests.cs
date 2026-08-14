using Light.Smtp;

namespace UnitTests.SmtpMailTests;

public class SmtpMailTests
{
    private readonly SmtpNetMailSender _smtpMail;
    private readonly string _fromMail;

    public SmtpMailTests()
    {
        _fromMail = "user@domain.local";

        var host = "smtp.freesmtpservers.com";

        _smtpMail = new SmtpNetMailSender(host)
        {
            UseSsl = false, // Set to true if your SMTP server requires SSL
        };
    }

    [Test]
    public async Task Must_Send_Email_With_No_Exceptions()
    {
        var recipients = new List<string>
        {
            "user@domain.local"
        };

        await _smtpMail.SendAsync(
            _fromMail,
            _fromMail,
            recipients,
            "Test Email",
            "<h1>Hello World</h1><p>This is a test email.</p>");
    }
}
