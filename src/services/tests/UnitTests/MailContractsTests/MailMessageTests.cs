using Light.Mail;
using NUnit.Framework;

namespace UnitTests.MailContractsTests;

public class MailMessageTests
{
    [Test]
    public void New_MailMessage_OptionalCollections_DefaultToNull()
    {
        var message = new MailMessage
        {
            Recipients = ["user@domain.local"],
            Subject = "Subject",
            Content = "Content",
        };

        Assert.That(message.CcRecipients, Is.Null);
        Assert.That(message.BccRecipients, Is.Null);
        Assert.That(message.Attachments, Is.Null);
    }
}
