using Light.EntityFrameworkCore.Extensions;

namespace EntityFrameworkCore.Tests;

public class QueryableWithNoLockExtensionsTests : TestFixtureBase
{
    [Test]
    public async Task CountWithNoLockAsync_Should_Count_Entity_Query()
    {
        var count = await Context.Products.CountWithNoLockAsync();

        count.ShouldBe(5);
    }

    [Test]
    public async Task CountWithNoLockAsync_Should_Count_Filtered_Entity_Query()
    {
        var count = await Context.Products.Where(x => x.IsActive).CountWithNoLockAsync();

        count.ShouldBe(3);
    }
}
