using MassTransit;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Light.MassTransit.RabbitMQ
{
    public class MassTransitConfigurator
    {
        internal List<(Type, Type)> Consumers = new List<(Type, Type)>();

        internal RabbitMQConfigurator RabbitMQConfigurator { get; set; } = new RabbitMQConfigurator();

        internal Assembly[] FromAssemblies { get; set; } = Array.Empty<Assembly>();

        public void AddConsumer<TConsumer, TDefinition>()
            where TConsumer : class, IConsumer
            where TDefinition : class, IConsumerDefinition<TConsumer>
        {
            Consumers.Add((typeof(TConsumer), typeof(TDefinition)));
        }

        public void AddConsumers(params Assembly[] assemblies)
        {
            FromAssemblies = assemblies;
        }

        public void ConfigRabbitMQ(Action<RabbitMQConfigurator> action)
        {
            action(RabbitMQConfigurator);
        }
    }
}
