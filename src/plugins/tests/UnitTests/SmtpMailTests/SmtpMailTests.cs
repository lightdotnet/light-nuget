using Light.Smtp;

namespace UnitTests.SmtpMailTests;

/// <summary>
/// Integration test against the public smtp.freesmtpservers.com server (network I/O).
/// Set SMTP_PUBLIC_TEST=1 to run; ignored otherwise.
/// </summary>
[Category("Integration")]
public class SmtpMailTests
{
    private SmtpMailSender _smtpMail = null!;
    private string _fromMail = null!;

    [SetUp]
    public void SetUp()
    {
        if (Environment.GetEnvironmentVariable("SMTP_PUBLIC_TEST") != "1")
        {
            Assert.Ignore("SMTP_PUBLIC_TEST=1 not set.");
        }

        _fromMail = "user@domain.local";

        var host = "smtp.freesmtpservers.com";

        _smtpMail = new SmtpMailSender(host)
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
