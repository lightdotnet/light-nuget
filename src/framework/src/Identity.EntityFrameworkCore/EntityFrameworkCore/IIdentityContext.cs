namespace Light.Identity.EntityFrameworkCore;

public interface IIdentityContext
{
    DbSet<User> Users { get; }

    DbSet<UserRole> UserRoles { get; }

    DbSet<JwtToken> JwtTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
