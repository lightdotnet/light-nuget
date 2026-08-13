using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.Tests;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductName).HasMaxLength(200);
        });
    }
}