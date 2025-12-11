using System;
using System.Collections.Generic;

namespace Light.MassTransit.RabbitMQ
{
    public class RabbitMQConfigurator
    {
        internal List<Type> ExcludeTypes = new List<Type>();

        public string Host { get; set; } = null!;

        public string Username { get; set; } = null!;

        public string Password { get; set; } = null!;

        /// <summary>
        /// Exclude type of T auto add to topic/exchange
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Exclude<T>() => ExcludeTypes.Add(typeof(T));

        /// <summary>
        /// Exclude type auto add to topic/exchange
        /// </summary>
        /// <param name="type"></param>
        public void Exclude(Type type) => ExcludeTypes.Add(type);
    }
}
