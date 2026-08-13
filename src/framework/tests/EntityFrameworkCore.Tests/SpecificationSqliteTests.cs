using Light.EntityFrameworkCore.Extensions;

namespace EntityFrameworkCore.Tests;

public class SpecificationSqliteTests : SqliteTestFixtureBase
{
    [Test]
    public async Task DbSet_ToListAsync_With_Boxed_OrderByDescending_Should_Translate_And_Order_Correctly()
    {
        var spec = new ProductOrderByPriceDescSpec();

        var result = await Context.Set<Product>().ToListAsync(spec);

        result.Count.ShouldBe(5);
        result[0].Price.ShouldBe(50m);
        result[4].Price.ShouldBe(10m);
    }
}
