using MassTransit;

namespace Light.AspNetCore.Modularity
{
    internal interface IModuleConsumer
    {
        /// <summary>
        /// Add Module MassTransit Consumers
        /// </summary>
        void AddConsumers(IBusRegistrationConfigurator configurator);
    }

    public abstract class ModuleConsumer : IModuleConsumer
    {
        public virtual void AddConsumers(IBusRegistrationConfigurator configurator)
        { }
    }
}