using Light.EventBus.Events;
using MassTransit;
using System.Linq;
using System.Reflection;

namespace Light.MassTransit.RabbitMQ
{
    internal static class BindingNameExtensions
    {
        internal static string? GetBindingName(this MemberInfo memberInfo)
        {
            var typeOfBindingNameAttribute = typeof(BindingNameAttribute);

            var instance = memberInfo.GetCustomAttributes(typeOfBindingNameAttribute, true).FirstOrDefault() as BindingNameAttribute;

            return instance?.BindingName;
        }

    }

    public class BusEntityBindingNameFormatter : IEntityNameFormatter
    {
        private readonly IEntityNameFormatter _original;

        public BusEntityBindingNameFormatter(IEntityNameFormatter original)
        {
            _original = original;
        }

        public string FormatEntityName<T>()
        {
            var name = typeof(T).GetBindingName() ?? _original.FormatEntityName<T>();

            return name;
        }
    }
}