using Light.Smtp;

namespace UnitTests.SmtpMailTests;

/// <summary>
/// Integration test against a real SMTP server (e.g. a new ethereal.email account).
/// Set SMTP_TEST_USERNAME / SMTP_TEST_PASSWORD (and optionally SMTP_TEST_HOST) env vars to run; ignored otherwise.
/// </summary>
[Category("Integration")]
public class SmtpMailKitTests
{
    private SmtpMailKitSender _smtpMailKit = null!;
    private string _fromMail = null!;

    [SetUp]
    public void SetUp()
    {
        var host = Environment.GetEnvironmentVariable("SMTP_TEST_HOST") ?? "smtp.ethereal.email";
        var userName = Environment.GetEnvironmentVariable("SMTP_TEST_USERNAME");
        var password = Environment.GetEnvironmentVariable("SMTP_TEST_PASSWORD");

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            Assert.Ignore("SMTP_TEST_USERNAME / SMTP_TEST_PASSWORD not set.");
        }

        _fromMail = userName;

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
