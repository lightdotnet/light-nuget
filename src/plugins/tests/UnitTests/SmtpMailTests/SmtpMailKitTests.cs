using Light.Smtp;

namespace UnitTests.SmtpMailTests;

public class SmtpMailKitTests
{
    private readonly SmtpMailKitSender _smtpMailKit;
    private readonly string _fromMail;

    /// <summary>
    /// Please config new ethereal before Tests
    /// </summary>
    public SmtpMailKitTests()
    {
        _fromMail = "waino.kuhlman@ethereal.email";

        var host = "smtp.ethereal.email";
        var userName = _fromMail;
        var password = "RUMp811zYYVkPuvcdY";

        _smtpMailKit = new SmtpMailKitSender(host, userName, password)
        {
            UseSsl = false,
        };
    }

    [Test]
    public async Task Must_Send_Email_With_No_Exceptions()
    {
        var recipients = new List<string>
        {
            "user@domain.local"
        };

        await _smtpMailKit.SendAsync(
            _fromMail,
            _fromMail,
            recipients,
            "Test Email",
            "<h1>Hello World</h1><p>This is a test email.</p>");
    }
}
