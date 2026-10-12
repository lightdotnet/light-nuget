using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Adds assemblies to scan for <c>ModuleConsumer</c> types. Can be called multiple times; assemblies are
        /// appended to those added by previous calls (duplicates are ignored).
        /// </summary>
        public void AddConsumers(params Assembly[] assemblies)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            FromAssemblies = FromAssemblies
                .Concat(assemblies.Where(x => x != null))
                .Distinct()
                .ToArray();
        }

        public void ConfigRabbitMQ(Action<RabbitMQConfigurator> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            action(RabbitMQConfigurator);
        }
    }
}
