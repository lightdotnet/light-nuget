using Light.ActiveDirectory.Dtos;
using NUnit.Framework;

namespace UnitTests.ActiveDirectoryTests;

public class DomainUserDtoTests
{
    [Test]
    public void Constructor_SetsUserName()
    {
        var dto = new DomainUserDto("user1");

        dto.UserName.ShouldBe("user1");
    }

    [Test]
    public void OptionalProperties_DefaultToNull()
    {
        var dto = new DomainUserDto("user1");

        Assert.That(dto.FirstName, Is.Null);
        Assert.That(dto.LastName, Is.Null);
        Assert.That(dto.Email, Is.Null);
        Assert.That(dto.PhoneNumber, Is.Null);
    }

    [Test]
    public void OptionalProperties_AreMutableAfterConstruction()
    {
        var dto = new DomainUserDto("user1")
        {
            FirstName = "First",
            LastName = "Last",
        };

        dto.FirstName.ShouldBe("First");
        dto.LastName.ShouldBe("Last");
    }

    [Test]
    public void RecordEquality_ComparesAllMembers()
    {
        var dto1 = new DomainUserDto("user1") { FirstName = "First" };
        var dto2 = new DomainUserDto("user1") { FirstName = "Second" };

        Assert.That(dto1, Is.Not.EqualTo(dto2));
    }
}
