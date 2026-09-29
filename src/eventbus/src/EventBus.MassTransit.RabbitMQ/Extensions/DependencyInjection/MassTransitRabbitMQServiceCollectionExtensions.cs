using Light.AspNetCore.Modularity;
using Light.EventBus.Abstractions;
using Light.EventBus.Events;
using Light.Extensions.DependencyInjection;
using Light.MassTransit.RabbitMQ;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Light.Extensions.DependencyInjection
{
    public static class MassTransitRabbitMQServiceCollectionExtensions
    {
        private static IRabbitMqBusFactoryConfigurator UseRabbitMQ(
            this IRabbitMqBusFactoryConfigurator rabbitMqBusFactoryConfigurator,
            IBusRegistrationContext busRegistrationContext,
            RabbitMQConfigurator configurator)
        {
            rabbitMqBusFactoryConfigurator.Host(configurator.Host, x =>
            {
                x.Username(configurator.Username);
                x.Password(configurator.Password);
            });

            // topology (entity names, publish excludes) must be configured before the receive endpoints are created,
            // otherwise the endpoints are bound using the default formatter/topology.
            var nameFormatter = new BusEntityBindingNameFormatter(rabbitMqBusFactoryConfigurator.MessageTopology.EntityNameFormatter);
            rabbitMqBusFactoryConfigurator.MessageTopology.SetEntityNameFormatter(nameFormatter);

            rabbitMqBusFactoryConfigurator.Publish<IIntegrationEvent>(p => p.Exclude = true);

            foreach (var excludeType in configurator.ExcludeTypes)
            {
                // exclude IntegrationEvent auto create to topic/exchange
                rabbitMqBusFactoryConfigurator.Publish(excludeType, p => p.Exclude = true);
            }

            rabbitMqBusFactoryConfigurator.ConfigureEndpoints(busRegistrationContext);

            return rabbitMqBusFactoryConfigurator;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // some types could not be loaded (e.g. a missing optional dependency) - scan the ones that could.
                return ex.Types.Where(x => x != null)!;
            }
        }

        private static IBusRegistrationConfigurator AddModuleConsumers(
            this IBusRegistrationConfigurator configurator,
            params Assembly[] assemblies)
        {
            // get all classes inherit from interface
            var moduleConsumerTypes = assemblies
                .SelectMany(GetLoadableTypes)
                .Where(x =>
                    typeof(IModuleConsumer).IsAssignableFrom(x)
                    && x.IsClass && !x.IsAbstract);

            foreach (var moduleConsumerType in moduleConsumerTypes)
            {
                IModuleConsumer instance;
                try
                {
                    instance = (IModuleConsumer)Activator.CreateInstance(moduleConsumerType)!;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to create an instance of module consumer '{moduleConsumerType.FullName}'. " +
                        "It must have a public parameterless constructor.", ex);
                }

                instance.AddConsumers(configurator);
            }

            return configurator;
        }

        private static void Validate(RabbitMQConfigurator configurator, string paramName)
        {
            if (string.IsNullOrWhiteSpace(configurator.Host))
            {
                throw new ArgumentException(
                    $"RabbitMQ setting '{nameof(RabbitMQConfigurator.Host)}' is required. Set it via ConfigRabbitMQ(...).",
                    paramName);
            }

            if (string.IsNullOrWhiteSpace(configurator.Username))
            {
                throw new ArgumentException(
                    $"RabbitMQ setting '{nameof(RabbitMQConfigurator.Username)}' is required. Set it via ConfigRabbitMQ(...).",
                    paramName);
            }

            if (string.IsNullOrEmpty(configurator.Password))
            {
                throw new ArgumentException(
                    $"RabbitMQ setting '{nameof(RabbitMQConfigurator.Password)}' is required. Set it via ConfigRabbitMQ(...).",
                    paramName);
            }
        }

        /// <summary>
        /// Registers MassTransit with the RabbitMQ transport, the configured consumers and
        /// <see cref="IEventBus"/> (<see cref="RabbitMQEventBus"/>, scoped).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="action"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <see cref="RabbitMQConfigurator.Host"/>, <see cref="RabbitMQConfigurator.Username"/> or
        /// <see cref="RabbitMQConfigurator.Password"/> was not set by <paramref name="action"/>.
        /// </exception>
        public static IServiceCollection AddRabbitMQEventBus(
            this IServiceCollection services,
            Action<MassTransitConfigurator> action)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var massTransitConfigurator = new MassTransitConfigurator();
            action(massTransitConfigurator);

            Validate(massTransitConfigurator.RabbitMQConfigurator, nameof(action));

            services.AddMassTransit((IBusRegistrationConfigurator x) =>
            {
                foreach (var consumer in massTransitConfigurator.Consumers)
                {
                    x.AddConsumer(consumer.Item1, consumer.Item2);
                }

                if (massTransitConfigurator.FromAssemblies.Length > 0)
                {
                    x.AddModuleConsumers(massTransitConfigurator.FromAssemblies);
                }

                x.SetKebabCaseEndpointNameFormatter();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.UseRabbitMQ(context, massTransitConfigurator.RabbitMQConfigurator);
                });
            });

            services.AddScoped<IEventBus, RabbitMQEventBus>();

            return services;
        }

        /// <summary>
        /// Same as <see cref="AddRabbitMQEventBus"/>. Prefer <see cref="AddRabbitMQEventBus"/>: this name overlaps
        /// MassTransit's own <c>AddMassTransit(IServiceCollection, Action&lt;IBusRegistrationConfigurator&gt;)</c>, so
        /// which one a call binds to depends on the lambda body.
        /// </summary>
        public static IServiceCollection AddMassTransit(
            this IServiceCollection services,
            Action<MassTransitConfigurator> action)
        {
            return services.AddRabbitMQEventBus(action);
        }
    }
}
