using Light.ActiveDirectory;
using NUnit.Framework;

namespace UnitTests.ActiveDirectoryTests;

public class DomainOptionsTests
{
    [Test]
    public void DefaultName_IsDomainCom() => new DomainOptions().Name.ShouldBe("domain.com");

    [Test]
    public void Enable_WhenNamePopulated_IsTrue() =>
        new DomainOptions { Name = "company.local" }.Enable.ShouldBeTrue();

    [Test]
    public void Enable_WhenNameEmpty_IsFalse() =>
        new DomainOptions { Name = "" }.Enable.ShouldBeFalse();

    [Test]
    public void Enable_WhenNameWhitespace_IsTrue()
    {
        // Documents actual (arguably surprising) behavior: Enable uses !string.IsNullOrEmpty, not
        // IsNullOrWhiteSpace, so a whitespace-only Name is still treated as "configured".
        new DomainOptions { Name = "   " }.Enable.ShouldBeTrue();
    }
}

public class LdapOptionsTests
{
    [Test]
    public void DefaultValues_MatchExpected()
    {
        var options = new LdapOptions();

        options.Name.ShouldBe("domain.com");
        options.Address.ShouldBe("10.0.10.2");
        options.Port.ShouldBe(389);
        options.Connection.ShouldBe("LDAP://127.0.0.1/DC=company,DC=local");
        options.NewUserConnection.ShouldBe("LDAP://127.0.0.1/ou=new_users,DC=company,DC=local");
        options.UseSsl.ShouldBeFalse();
        options.UserName.ShouldBe(string.Empty);
        options.Password.ShouldBe(string.Empty);
    }
}

public class LdapFilterEscapeTests
{
    [TestCase("john.doe", "john.doe")]
    [TestCase("*", @"\2a")]
    [TestCase("adm*", @"adm\2a")]
    [TestCase("x)(objectClass=*", @"x\29\28objectClass=\2a")]
    [TestCase(@"a\b", @"a\5cb")]
    [TestCase("a\0b", @"a\00b")]
    public void EscapeLdapFilterValue_EscapesSpecialCharacters(string input, string expected)
    {
        Light.ActiveDirectory.Services.LDAPService.EscapeLdapFilterValue(input).ShouldBe(expected);
    }
}
