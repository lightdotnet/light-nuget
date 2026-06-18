using Light.EntityFrameworkCore.Repositories;

namespace EntityFrameworkCore.Tests;

public class RepositoryTests : TestFixtureBase
{
    private Repository<Product> _repo = default!;

    [SetUp]
    public new void SetUp() => _repo = new Repository<Product>(Context);

    #region [Query — Include / Where / WhereIf]

    [Test]
    public void Include_Should_Return_Queryable()
    {
        var query = _repo.Include(x => x.ProductName!);
        query.ShouldNotBeNull();
    }

    [Test]
    public void Where_Expression_Should_Filter()
    {
        var result = _repo.Where(x => x.Id == 1).ToList();
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(1);
    }

    [Test]
    public void Where_Spec_Should_Filter()
    {
        var spec = new ProductByIdSpec(2);
        var result = _repo.Where(spec).ToList();
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(2);
    }

    [Test]
    public void WhereIf_Expression_True_Should_Filter()
    {
        var result = _repo.WhereIf(true, x => x.Id > 3).ToList();
        result.Count.ShouldBe(2);
    }

    [Test]
    public void WhereIf_Expression_False_Should_Return_All()
    {
        var result = _repo.WhereIf(false, x => x.Id > 3).ToList();
        result.Count.ShouldBe(5);
    }

    [Test]
    public void WhereIf_Spec_True_Should_Filter()
    {
        var spec = new ProductHaveIdGreaterThanSpec(3);
        var result = _repo.WhereIf(true, spec).ToList();
        result.Count.ShouldBe(2);
    }

    [Test]
    public void WhereIf_Spec_False_Should_Return_All()
    {
        var spec = new ProductHaveIdGreaterThanSpec(3);
        var result = _repo.WhereIf(false, spec).ToList();
        result.Count.ShouldBe(5);
    }

    #endregion

    #region [Query — ToListAsync / FindAsync]

    [Test]
    public async Task ToListAsync_Should_Return_All()
    {
        var result = await _repo.ToListAsync();
        result.Count.ShouldBe(5);
    }

    [Test]
    public async Task FindAsync_Should_Return_Entity()
    {
        var entity = await _repo.FindAsync(1);
        entity.ShouldNotBeNull();
        entity!.Id.ShouldBe(1);
    }

    [Test]
    public async Task FindAsync_Should_Return_Null_When_NotFound()
    {
        var entity = await _repo.FindAsync(999);
        entity.ShouldBeNull();
    }

    [Test]
    public async Task FindAsync_ObjectArray_Should_Work()
    {
        var entity = await _repo.FindAsync([3]);
        entity.ShouldNotBeNull();
        entity!.Id.ShouldBe(3);
    }

    #endregion

    #region [Command — Add / Update / Remove]

    [Test]
    public async Task Add_Should_Track_Entity()
    {
        var product = new Product { Id = 100, ProductName = "New", Price = 99, IsActive = true };
        _repo.Add(product);
        await Context.SaveChangesAsync();
        var found = await _repo.FindAsync(100);
        found.ShouldNotBeNull();
    }

    [Test]
    public async Task AddRange_Should_Track_Entities()
    {
        var products = new[]
        {
            new Product { Id = 101, ProductName = "A", Price = 1, IsActive = true },
            new Product { Id = 102, ProductName = "B", Price = 2, IsActive = false },
        };
        _repo.AddRange(products);
        await Context.SaveChangesAsync();
        var result = await _repo.ToListAsync();
        result.Count.ShouldBe(7);
    }

    [Test]
    public async Task Update_Should_Modify_Entity()
    {
        var entity = await _repo.FindAsync(1);
        entity!.ProductName = "Updated";
        _repo.Update(entity);
        await Context.SaveChangesAsync();
        var updated = await _repo.FindAsync(1);
        updated!.ProductName.ShouldBe("Updated");
    }

    [Test]
    public async Task UpdateRange_Should_Modify_Entities()
    {
        var entities = _repo.Where(x => x.Id <= 2).ToList();
        entities.ForEach(e => e.ProductName = "Bulk");
        _repo.UpdateRange(entities);
        await Context.SaveChangesAsync();
        var result = _repo.Where(x => x.ProductName == "Bulk").ToList();
        result.Count.ShouldBe(2);
    }

    [Test]
    public async Task Remove_Should_Delete_Entity()
    {
        var entity = await _repo.FindAsync(5);
        _repo.Remove(entity!);
        await Context.SaveChangesAsync();
        var result = await _repo.ToListAsync();
        result.Count.ShouldBe(4);
    }

    [Test]
    public async Task RemoveRange_Should_Delete_Entities()
    {
        var entities = _repo.Where(x => x.Id >= 4).ToList();
        _repo.RemoveRange(entities);
        await Context.SaveChangesAsync();
        var result = await _repo.ToListAsync();
        result.Count.ShouldBe(3);
    }

    [Test]
    public async Task AddAsync_Should_Track_Entity()
    {
        var product = new Product { Id = 200, ProductName = "Async", Price = 10, IsActive = true };
        await _repo.AddAsync(product);
        await Context.SaveChangesAsync();
        var found = await _repo.FindAsync(200);
        found.ShouldNotBeNull();
    }

    [Test]
    public async Task AddRangeAsync_Should_Track_Entities()
    {
        var products = new[]
        {
            new Product { Id = 201, ProductName = "X", Price = 1, IsActive = true },
            new Product { Id = 202, ProductName = "Y", Price = 2, IsActive = false },
        };
        await _repo.AddRangeAsync(products);
        await Context.SaveChangesAsync();
        var result = await _repo.ToListAsync();
        result.Count.ShouldBe(7);
    }

    #endregion

    #region [Query — CountAsync / AnyAsync — P2]

    [Test]
    public async Task CountAsync_Should_Return_Total()
    {
        var count = await _repo.CountAsync();
        count.ShouldBe(5);
    }

    [Test]
    public async Task AnyAsync_True_Should_Return_True()
    {
        var result = await _repo.AnyAsync(x => x.Price > 10m);
        result.ShouldBeTrue();
    }

    [Test]
    public async Task AnyAsync_False_Should_Return_False()
    {
        var result = await _repo.AnyAsync(x => x.Price > 999m);
        result.ShouldBeFalse();
    }

    #endregion
}