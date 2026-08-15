using Microsoft.Extensions.DependencyInjection;

namespace Light.Extensions.DependencyInjection
{
    public static class AutoAddServiceExtensions
    {
        public static IServiceCollection AutoAddDependencies(this IServiceCollection services)
        {
            // scan every loaded assembly once and reuse the result for all three lifetimes
            var candidateTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(x => x.IsClass && !x.IsAbstract)
                .ToList();

            return services
                .AddServices(candidateTypes, typeof(ITransientDependency), ServiceLifetime.Transient)
                .AddServices(candidateTypes, typeof(IScopedDependency), ServiceLifetime.Scoped)
                .AddServices(candidateTypes, typeof(ISingletonDependency), ServiceLifetime.Singleton);
        }

        private static IServiceCollection AddServices(this IServiceCollection services, IReadOnlyCollection<Type> candidateTypes, Type typeOfDependency, ServiceLifetime lifetime)
        {
            // get all classes inherit from Dependency
            var allAssignableFromDependency = candidateTypes.Where(x => typeOfDependency.IsAssignableFrom(x));

            // select dependencies with interfaces
            // get interface matching with class implementation by exact naming convention
            //      ex: Interface: IOrderService => Class: OrderService
            var dependencies = allAssignableFromDependency
                .Select(s => new
                {
                    Interface = s
                        .GetInterfaces()
                        .FirstOrDefault(x => x != typeOfDependency && x.Name == "I" + s.Name),

                    Implementation = s
                });

            // inject dependencies
            foreach (var dependency in dependencies)
            {
                if (dependency.Interface is null)
                    services.AddService(dependency.Implementation, lifetime);
                else
                    services.AddService(dependency.Interface, dependency.Implementation, lifetime);
            }

            return services;
        }

        private static IServiceCollection AddService(this IServiceCollection services, Type interfaceType, Type implementationType, ServiceLifetime lifetime)
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

        private static IServiceCollection AddService(this IServiceCollection services, Type implementationType, ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    services.AddTransient(implementationType);
                    break;
                case ServiceLifetime.Scoped:
                    services.AddScoped(implementationType);
                    break;
                case ServiceLifetime.Singleton:
                    services.AddSingleton(implementationType);
                    break;
                default:
                    throw new ArgumentException("Invalid lifeTime", nameof(lifetime));
            }

            return services;
        }

    }
}

