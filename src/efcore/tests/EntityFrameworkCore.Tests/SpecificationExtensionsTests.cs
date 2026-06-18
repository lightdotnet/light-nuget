using Light.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.Tests;

public class SpecificationExtensionsTests : TestFixtureBase
{
    #region [DbSet — Data Methods]

    [Test]
    public async Task DbSet_ToListAsync_Should_Return_Filtered()
    {
        var spec = new ProductByIdSpec(1);
        var result = await Context.Set<Product>().ToListAsync(spec);
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(1);
    }

    [Test]
    public async Task DbSet_ToListAsync_NoTracking_Should_Return_Filtered()
    {
        var spec = new ProductByIdSpec(1);
        var result = await Context.Set<Product>().ToListAsync(spec, tracking: false);
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(1);
    }

    [Test]
    public async Task DbSet_SingleAsync_Should_Return_Single()
    {
        var spec = new ProductByIdSpec(2);
        var result = await Context.Set<Product>().SingleAsync(spec);
        result.Id.ShouldBe(2);
    }

    [Test]
    public async Task DbSet_SingleOrDefaultAsync_Should_Return_Entity()
    {
        var spec = new ProductByIdSpec(3);
        var result = await Context.Set<Product>().SingleOrDefaultAsync(spec);
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(3);
    }

    [Test]
    public async Task DbSet_SingleOrDefaultAsync_NoMatch_Should_Return_Null()
    {
        var spec = new ProductByIdSpec(999);
        var result = await Context.Set<Product>().SingleOrDefaultAsync(spec);
        result.ShouldBeNull();
    }

    [Test]
    public async Task DbSet_FirstAsync_Should_Return_First()
    {
        var spec = new ProductHaveIdGreaterThanSpec(3);
        var result = await Context.Set<Product>().FirstAsync(spec);
        result.ShouldNotBeNull();
    }

    [Test]
    public async Task DbSet_FirstOrDefaultAsync_Should_Return_Entity()
    {
        var spec = new ProductByIdSpec(4);
        var result = await Context.Set<Product>().FirstOrDefaultAsync(spec);
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(4);
    }

    [Test]
    public async Task DbSet_FirstOrDefaultAsync_NoTracking_Should_Work()
    {
        var spec = new ProductByIdSpec(5);
        var result = await Context.Set<Product>().FirstOrDefaultAsync(spec, tracking: false);
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(5);
    }

    #endregion

    #region [DbSet — Aggregate Methods]

    [Test]
    public async Task DbSet_AnyAsync_True_Should_Return_True()
    {
        var spec = new ProductByIdSpec(1);
        var result = await Context.Set<Product>().AnyAsync(spec);
        result.ShouldBeTrue();
    }

    [Test]
    public async Task DbSet_AnyAsync_False_Should_Return_False()
    {
        var spec = new ProductByIdSpec(999);
        var result = await Context.Set<Product>().AnyAsync(spec);
        result.ShouldBeFalse();
    }

    [Test]
    public async Task DbSet_CountAsync_Should_Return_Count()
    {
        var spec = new ProductHaveIdGreaterThanSpec(2);
        var result = await Context.Set<Product>().CountAsync(spec);
        result.ShouldBe(3);
    }

    #endregion

    #region [DbContext — Data Methods]

    [Test]
    public async Task DbContext_ToListAsync_Should_Return_Filtered()
    {
        var spec = new ProductByIdSpec(1);
        var result = await Context.ToListAsync(spec);
        result.Count.ShouldBe(1);
    }

    [Test]
    public async Task DbContext_SingleAsync_Should_Return_Single()
    {
        var spec = new ProductByIdSpec(3);
        var result = await Context.SingleAsync(spec);
        result.Id.ShouldBe(3);
    }

    [Test]
    public async Task DbContext_SingleOrDefaultAsync_Should_Return_Entity()
    {
        var spec = new ProductByIdSpec(2);
        var result = await Context.SingleOrDefaultAsync(spec);
        result.ShouldNotBeNull();
    }

    [Test]
    public async Task DbContext_FirstAsync_Should_Return_First()
    {
        var spec = new ProductHaveIdGreaterThanSpec(0);
        var result = await Context.FirstAsync(spec);
        result.ShouldNotBeNull();
    }

    [Test]
    public async Task DbContext_FirstOrDefaultAsync_Should_Return_Entity()
    {
        var spec = new ProductByIdSpec(5);
        var result = await Context.FirstOrDefaultAsync(spec);
        result.ShouldNotBeNull();
    }

    #endregion

    #region [DbContext — Aggregate Methods]

    [Test]
    public async Task DbContext_AnyAsync_Should_Work()
    {
        var spec = new ProductByIdSpec(1);
        var result = await Context.AnyAsync(spec);
        result.ShouldBeTrue();
    }

    [Test]
    public async Task DbContext_CountAsync_Should_Work()
    {
        var spec = new ProductHaveIdGreaterThanSpec(0);
        var result = await Context.CountAsync(spec);
        result.ShouldBe(5);
    }

    #endregion

    #region [Apply — OrderBy + Paging — P3]

    [Test]
    public async Task DbSet_ToListAsync_With_OrderBy_Spec()
    {
        var spec = new ProductOrderByPriceDescSpec();
        var result = await Context.Set<Product>().ToListAsync(spec);
        result.Count.ShouldBe(5);
        result[0].Price.ShouldBe(50m); // highest price first
        result[4].Price.ShouldBe(10m); // lowest price last
    }

    [Test]
    public async Task DbSet_ToListAsync_With_Paging_Spec()
    {
        // Active products (Id 1,2,4) sorted by Price ASC: 10,20,40
        // Skip 0, Take 2 → Price 10, 20
        var spec = new ProductPagedByPriceSpec(0, 2);
        var result = await Context.Set<Product>().ToListAsync(spec);
        result.Count.ShouldBe(2);
        result[0].Price.ShouldBe(10m);
        result[1].Price.ShouldBe(20m);
    }

    [Test]
    public async Task DbContext_CountAsync_With_GreaterThan_Spec()
    {
        var spec = new ProductHaveIdGreaterThanSpec(3);
        var result = await Context.CountAsync(spec);
        result.ShouldBe(2); // Id 4 and 5
    }

    #endregion
}