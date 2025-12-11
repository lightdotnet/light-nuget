using Light.Mail;
using Light.SmtpMail;

namespace UnitTests.SmtpMailTests;

public class SmtpMailTests
{
    private readonly SmtpMail _smtpMail;
    private readonly string _fromMail;

    public SmtpMailTests()
    {
        _fromMail = "user@domain.local";

        var host = "smtp.freesmtpservers.com";

        _smtpMail = new SmtpMail(host)
        {
            UseSsl = false, // Set to true if your SMTP server requires SSL
        };
    }

    [Fact]
    public async Task Must_Send_Email_With_No_Exceptions()
    {
        var recipients = new List<string>
        {
            "user@domain.local"
        };

        var mail = new MailMessage
        {
            Recipients = recipients,
            Subject = "Test Email",
            Content = "<h1>Hello World</h1><p>This is a test email.</p>",
        };

        await _smtpMail.SendAsync(new MailFrom(_fromMail), mail);
    }
}
