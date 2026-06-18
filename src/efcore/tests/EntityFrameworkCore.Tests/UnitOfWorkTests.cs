using Light.EntityFrameworkCore.Repositories;
using Light.Repositories;

namespace EntityFrameworkCore.Tests;

public class UnitOfWorkTests : TestFixtureBase
{
    private UnitOfWork? _unitOfWork;

    [SetUp]
    public void CreateUnitOfWork()
    {
        _unitOfWork = new UnitOfWork(Context);
    }

    [TearDown]
    public void DisposeUnitOfWork()
    {
        _unitOfWork?.Dispose();
        _unitOfWork = null;
    }

    #region [Set<T>]

    [Test]
    public void Set_Should_Return_Repository()
    {
        var repo = _unitOfWork!.Set<Product>();

        repo.ShouldNotBeNull();
        repo.ShouldBeInstanceOf<IRepository<Product>>();
    }

    [Test]
    public void Set_Should_Cache_Repository()
    {
        var repo1 = _unitOfWork!.Set<Product>();
        var repo2 = _unitOfWork!.Set<Product>();

        Assert.That(repo1, Is.SameAs(repo2));
    }

    #endregion

    #region [SaveChanges]

    [Test]
    public void SaveChanges_Should_Persist()
    {
        _unitOfWork!.Set<Product>().Add(new Product { Id = 300, ProductName = "UoW Sync" });

        var affected = _unitOfWork!.SaveChanges();

        affected.ShouldBeGreaterThan(0);
        Context.ChangeTracker.Clear();
        Context.Products.Find(300).ShouldNotBeNull();
    }

    [Test]
    public async Task SaveChangesAsync_Should_Persist()
    {
        _unitOfWork!.Set<Product>().Add(new Product { Id = 301, ProductName = "UoW Async" });

        var affected = await _unitOfWork!.SaveChangesAsync();

        affected.ShouldBeGreaterThan(0);
        Context.ChangeTracker.Clear();
        (await Context.Products.FindAsync(301)).ShouldNotBeNull();
    }

    #endregion

    #region [Dispose]

    [Test]
    public void Dispose_Should_Not_Throw()
    {
        var uow = new UnitOfWork(Context);

        Assert.DoesNotThrow(() => uow.Dispose());
    }

    [Test]
    public async Task DisposeAsync_Should_Not_Throw()
    {
        var uow = new UnitOfWork(Context);

        Assert.DoesNotThrowAsync(async () => await uow.DisposeAsync());
    }

    #endregion
}