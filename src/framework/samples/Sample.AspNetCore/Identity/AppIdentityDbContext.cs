using Light.Domain.Entities.Interfaces;
using Light.Domain.ValueObjects;
using Light.Identity.EntityFrameworkCore;
using Light.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Sample.AspNetCore.Identity;

public class AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : IdentityDbContext(options)
{
    public override int SaveChanges()
    {
        AuditEntries("", DateTime.Now, true);
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AuditEntries("", DateTime.Now, true);
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Role>().ToTable(name: Tables.Roles, Schemas.Identity);

        builder.Entity<RoleClaim>().ToTable(name: Tables.RoleClaims, Schemas.Identity);

        builder.Entity<User>(entity =>
        {
            entity.ToTable(name: Tables.Users, Schemas.Identity);
        });

        builder.Entity<UserRole>().ToTable(name: Tables.UserRoles, Schemas.Identity);

        builder.Entity<UserLogin>().ToTable(name: Tables.UserLogins, Schemas.Identity);

        builder.Entity<UserClaim>().ToTable(name: Tables.UserClaims, Schemas.Identity);

        builder.Entity<UserToken>().ToTable(name: Tables.UserTokens, Schemas.Identity);
    }

    public void AuditEntries(string? userId, DateTimeOffset auditTime, bool enableSoftDelete = false)
    {
        var changeTracker = ChangeTracker;

        // fix null value when delete for Entities inherited ISoftDelete & ValueObjects
        changeTracker.Entries<ValueObject>()
            .Where(x => x.State is EntityState.Deleted)
            .ToList()
            .ForEach(e => e.State = EntityState.Unchanged);

        // auto set audit values for Auditable entities
        changeTracker.Entries<IAuditable>()
            .ToList()
            .ForEach(e =>
            {
                switch (e.State)
                {
                    case EntityState.Added:
                        e.Entity.Created = auditTime;
                        e.Entity.CreatedBy = userId;
                        break;

                    case EntityState.Modified:
                        e.Entity.LastModified = auditTime;
                        e.Entity.LastModifiedBy = userId;
                        break;

                    case EntityState.Deleted:
                        if (e.Entity is ISoftDelete softDelete && enableSoftDelete)
                        {
                            softDelete.Deleted = auditTime;
                            softDelete.DeletedBy = userId;
                            e.State = EntityState.Modified;
                        }
                        break;
                }
            });
    }
}