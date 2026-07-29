using Light.Mail;
using NUnit.Framework;

namespace UnitTests.MailContractsTests;

public class MailAttachmentTests
{
    [Test]
    public void Constructor_SetsFileNameAndFileToBytes()
    {
        byte[] bytes = [1, 2, 3];

        var attachment = new MailAttachment("report.pdf", bytes);

        attachment.FileName.ShouldBe("report.pdf");
        attachment.FileToBytes.ShouldBe(bytes);
    }

    [Test]
    public void Constructor_WithEmptyByteArray_IsAccepted()
    {
        var attachment = new MailAttachment("empty.txt", []);

        Assert.That(attachment.FileToBytes, Is.Not.Null);
        Assert.That(attachment.FileToBytes, Is.Empty);
    }
}
