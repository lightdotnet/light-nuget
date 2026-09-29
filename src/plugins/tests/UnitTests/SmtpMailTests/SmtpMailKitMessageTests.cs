using Light.Extensions.DependencyInjection;
using Light.Smtp;
using MailKit.Security;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;

namespace UnitTests.SmtpMailTests;

/// <summary>
/// Offline tests (no network): message construction, TLS mode mapping and DI validation.
/// </summary>
public class SmtpMailKitMessageTests
{
    [Test]
    public void BuildMessage_SetsFromAndSender_WithDisplayName()
    {
        var message = SmtpMailKitSender.BuildMessage(
            "no-reply@example.com", "Example App", ["user@example.com"], "Subject", "<p>Hi</p>");

        var from = message.From.Mailboxes.Single();
        Assert.Multiple(() =>
        {
            Assert.That(from.Address, Is.EqualTo("no-reply@example.com"));
            Assert.That(from.Name, Is.EqualTo("Example App"));
            Assert.That(message.Sender?.Address, Is.EqualTo("no-reply@example.com"));
            Assert.That(message.Subject, Is.EqualTo("Subject"));
            Assert.That(message.HtmlBody, Is.EqualTo("<p>Hi</p>"));
        });
    }

    [Test]
    public void BuildMessage_AddsRecipientsCcBccAndAttachments()
    {
        var message = SmtpMailKitSender.BuildMessage(
            "no-reply@example.com", "Example App",
            ["a@example.com", "b@example.com"], "Subject", "Body",
            cc: ["c@example.com"],
            bcc: ["d@example.com"],
            attachments: new Dictionary<string, byte[]> { ["report.txt"] = [1, 2, 3] });

        Assert.Multiple(() =>
        {
            Assert.That(message.To.Mailboxes.Select(m => m.Address), Is.EqualTo(new[] { "a@example.com", "b@example.com" }));
            Assert.That(message.Cc.Mailboxes.Select(m => m.Address), Is.EqualTo(new[] { "c@example.com" }));
            Assert.That(message.Bcc.Mailboxes.Select(m => m.Address), Is.EqualTo(new[] { "d@example.com" }));
            Assert.That(message.Attachments.OfType<MimePart>().Select(a => a.FileName), Is.EqualTo(new[] { "report.txt" }));
        });
    }

    [TestCase(false, 587, SecureSocketOptions.StartTlsWhenAvailable)]
    [TestCase(false, 25, SecureSocketOptions.StartTlsWhenAvailable)]
    [TestCase(true, 587, SecureSocketOptions.StartTls)]
    [TestCase(true, 465, SecureSocketOptions.SslOnConnect)]
    public void ResolveSecureSocketOptions_MapsUseSslAndPort(bool useSsl, int port, SecureSocketOptions expected)
    {
        var sender = new SmtpMailKitSender("smtp.example.com", "user", "pass", port) { UseSsl = useSsl };

        Assert.That(sender.ResolveSecureSocketOptions(), Is.EqualTo(expected));
    }

    [Test]
    public void ResolveSecureSocketOptions_ExplicitValue_Overrides()
    {
        var sender = new SmtpMailKitSender("smtp.example.com", "user", "pass", 587)
        {
            UseSsl = true,
            SecureSocketOptions = SecureSocketOptions.None,
        };

        Assert.That(sender.ResolveSecureSocketOptions(), Is.EqualTo(SecureSocketOptions.None));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void AddSmtpMailKit_EmptyHost_Throws(string? host)
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddSmtpMailKit(o => o.Host = host!));
    }

    [Test]
    public void AddSmtpMail_EmptyHost_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddSmtpMail(o => o.Host = ""));
    }

    [Test]
    public void AddSmtpMailKit_ValidOptions_ResolvesSenderWithOptions()
    {
        var services = new ServiceCollection();
        services.AddSmtpMailKit(o =>
        {
            o.Host = "smtp.example.com";
            o.Port = 465;
            o.UseSsl = true;
            o.SecureSocketOptions = SecureSocketOptions.Auto;
        });

        var sender = services.BuildServiceProvider().GetRequiredService<ISmtpMailSender>();

        Assert.That(sender, Is.TypeOf<SmtpMailKitSender>());
        Assert.That(((SmtpMailKitSender)sender).ResolveSecureSocketOptions(), Is.EqualTo(SecureSocketOptions.Auto));
    }
}
