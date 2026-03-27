using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Light.Identity.EntityFrameworkCore;

public abstract class IdentityContext(DbContextOptions options) :
    IdentityDbContext<User, Role, string, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>(options),
    IIdentityContext
{
    public virtual DbSet<JwtToken> JwtTokens => Set<JwtToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            // Configure a relationship where the ActiveStatus is owned by (or part of) User.
            entity.OwnsOne(o => o.Status).Property(p => p.Value).HasColumnName("Status");
            entity.Navigation(emp => emp.Status).IsRequired();
        });

        builder.Entity<JwtToken>(e =>
        {
            e.HasIndex(i => i.UserId);
        });
    }
}
