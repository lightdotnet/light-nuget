using Light.Mail;
using NUnit.Framework;

namespace UnitTests.MailContractsTests;

public class MailFromTests
{
    [Test]
    public void Constructor_WithAddressOnly_SetsAddress_AndDisplayNameIsNull()
    {
        var from = new MailFrom("user@domain.local");

        from.Address.ShouldBe("user@domain.local");
        Assert.That(from.DisplayName, Is.Null);
    }

    [Test]
    public void Constructor_WithAddressAndDisplayName_SetsBothProperties()
    {
        var from = new MailFrom("user@domain.local", "User");

        from.Address.ShouldBe("user@domain.local");
        from.DisplayName.ShouldBe("User");
    }

    [Test]
    public void Constructor_WithExplicitNullDisplayName_LeavesDisplayNameNull()
    {
        var from = new MailFrom("user@domain.local", null);

        Assert.That(from.DisplayName, Is.Null);
    }
}
