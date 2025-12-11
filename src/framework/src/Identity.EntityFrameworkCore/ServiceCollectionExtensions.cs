using Light.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Identity;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// AddIdentityCore with DbContext Stores, Please AddAuthentication before this
    /// </summary>
    public static IdentityBuilder AddIdentity<TContext>(this IServiceCollection services)
        where TContext : IdentityContext
    {
        return services
            .AddIdentity<TContext>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false;

                // Password settings
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 3;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;

                // Lockout settings
                //options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(1);
                //options.Lockout.MaxFailedAccessAttempts = 10;

                // User settings
                options.User.RequireUniqueEmail = false;
            });
    }

    /// <summary>
    /// AddIdentityCore with DbContext Stores, Please AddAuthentication before this
    /// </summary>
    public static IdentityBuilder AddIdentity<TContext>(this IServiceCollection services, Action<IdentityOptions> options)
        where TContext : IdentityContext
    {
        var identityBuilder = services
            .AddIdentityCore<User>(options)
            .AddRoles<Role>()
            .AddEntityFrameworkStores<TContext>();

        services.AddScoped<IIdentityContext>(provider => provider.GetRequiredService<TContext>());

        return identityBuilder;
    }

    /// <summary>
    /// Add default UserManager service with Scoped lifetime
    /// </summary>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public static IServiceCollection AddDefaultUserManager(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        services.AddServiceLifetime(typeof(IUserService), typeof(UserService), lifetime);

        return services;
    }

    /// <summary>
    /// Add default RoleManager service with Scoped lifetime
    /// </summary>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public static IServiceCollection AddDefaultRoleManager(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        services.AddServiceLifetime(typeof(IRoleService), typeof(RoleService), lifetime);

        return services;
    }

    /// <summary>
    /// Register Jwt Token services
    /// </summary>
    public static IServiceCollection AddJwtTokenProvider(this IServiceCollection services)
    {
        services.AddScoped<JwtTokenMananger>();

        return services;
    }

    private static IServiceCollection AddServiceLifetime(this IServiceCollection services, Type interfaceType, Type implementationType, ServiceLifetime lifetime)
    {
        switch (lifetime)
        {
            case ServiceLifetime.Transient:
                services.AddTransient(interfaceType, implementationType);
                break;
            case ServiceLifetime.Scoped:
                services.AddScoped(interfaceType, implementationType);
                break;
            case ServiceLifetime.Singleton:
                services.AddSingleton(interfaceType, implementationType);
                break;
            default:
                throw new ArgumentException("Invalid lifeTime", nameof(lifetime));
        }

        return services;
    }
}