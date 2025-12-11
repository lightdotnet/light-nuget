using Light.AspNetCore.Modularity;
using Light.EventBus.Abstractions;
using Light.EventBus.Events;
using Light.Extensions.DependencyInjection;
using Light.MassTransit.RabbitMQ;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System;
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
            rabbitMqBusFactoryConfigurator.ConfigureEndpoints(busRegistrationContext);

            var nameFormatter = new BusEntityBindingNameFormatter(rabbitMqBusFactoryConfigurator.MessageTopology.EntityNameFormatter);
            rabbitMqBusFactoryConfigurator.MessageTopology.SetEntityNameFormatter(nameFormatter);

            rabbitMqBusFactoryConfigurator.Publish<IIntegrationEvent>(p => p.Exclude = true);

            foreach (var excludeType in configurator.ExcludeTypes)
            {
                // exclude IntegrationEvent auto create to topic/exchange
                rabbitMqBusFactoryConfigurator.Publish(excludeType, p => p.Exclude = true);
            }

            return rabbitMqBusFactoryConfigurator;
        }

        private static IBusRegistrationConfigurator AddModuleConsumers(
            this IBusRegistrationConfigurator configurator,
            params Assembly[] assemblies)
        {
            // get all classes inherit from interface
            var moduleConsumers = assemblies
                .SelectMany(s => s.GetTypes())
                .Where(x =>
                    typeof(IModuleConsumer).IsAssignableFrom(x)
                    && x.IsClass && !x.IsAbstract)
                .Select(s => Activator.CreateInstance(s) as IModuleConsumer);

            foreach (var instance in moduleConsumers)
            {
                instance?.AddConsumers(configurator);
            }

            return configurator;
        }

        public static IServiceCollection AddMassTransit(
            this IServiceCollection services,
            Action<MassTransitConfigurator> action)
        {
            var massTransitConfigurator = new MassTransitConfigurator();
            action(massTransitConfigurator);

            services.AddMassTransit(x =>
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

            services.AddScoped<IEventBus, MessageQueueService>();

            return services;
        }
    }
}
