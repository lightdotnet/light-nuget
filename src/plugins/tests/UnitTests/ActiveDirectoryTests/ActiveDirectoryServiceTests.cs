using Light.ActiveDirectory;
using Light.ActiveDirectory.Services;
using NUnit.Framework;

namespace UnitTests.ActiveDirectoryTests;

public class ActiveDirectoryServiceTests
{
    // Constructing ActiveDirectoryService and calling IsConfigured() performs no directory I/O.
    [TestCase("company.local", true)]
    [TestCase("", false)]
    [TestCase("   ", false)]
    [TestCase("domain.com", false)]
    [TestCase("DOMAIN.COM", false)]
    public void IsConfigured_RejectsEmptyAndPlaceholderNames(string name, bool expected)
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("ActiveDirectoryService is Windows-only.");
            return;
        }

        new ActiveDirectoryService(new DomainOptions { Name = name }).IsConfigured().ShouldBe(expected);
    }

    [Test]
    public void IsConfigured_WithDefaultOptions_IsFalse()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("ActiveDirectoryService is Windows-only.");
            return;
        }

        new ActiveDirectoryService(new DomainOptions()).IsConfigured().ShouldBeFalse();
    }
}
