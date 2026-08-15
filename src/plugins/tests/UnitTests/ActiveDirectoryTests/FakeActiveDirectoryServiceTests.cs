using Light.ActiveDirectory.Services;
using NUnit.Framework;

namespace UnitTests.ActiveDirectoryTests;

public class FakeActiveDirectoryServiceTests
{
    private readonly FakeActiveDirectoryService _service = new();

    [Test]
    public void IsConfigured_ReturnsFalse() => _service.IsConfigured().ShouldBeFalse();

    [Test]
    public async Task CheckPasswordSignInAsync_ReturnsFalse() =>
        (await _service.CheckPasswordSignInAsync("user", "password")).ShouldBeFalse();

    [Test]
    public void ChangePassword_ReturnsFalse() => _service.ChangePassword("user", "newPassword").ShouldBeFalse();

    [Test]
    public async Task GetByUserNameAsync_ReturnsNull() =>
        Assert.That(await _service.GetByUserNameAsync("user"), Is.Null);
}
