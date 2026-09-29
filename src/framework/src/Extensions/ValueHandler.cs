using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Light.Extensions
{
    public static class ValueHandler
    {
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();

        // Only readable + writable, non-indexer properties can be safely read and assigned.
        private static PropertyInfo[] GetCachedProperties(Type type) =>
            PropertyCache.GetOrAdd(type, t => t.GetProperties()
                .Where(pi => pi.CanRead && pi.CanWrite && pi.GetIndexParameters().Length == 0)
                .ToArray());

        /// <summary>
        /// Trim limit lenght of string in object
        /// </summary>
        /// <remarks>
        /// Returns <paramref name="data"/> unchanged when it is null. Read-only properties and indexers are skipped.
        /// </remarks>
        public static T MaximumCharHandler<T>(this T data, int lenght)
        {
            if (data is null)
                return data;

            foreach (PropertyInfo pi in GetCachedProperties(data.GetType()))
            {
                if (pi.PropertyType != typeof(string))
                    continue;

                var value = (string?)pi.GetValue(data, null);

                if (!string.IsNullOrEmpty(value))
                {
                    pi.SetValue(data, value!.Left(lenght));
                }
            }

            return data;
        }

        /// <summary>
        /// Change null Datetime values to default time in object
        /// </summary>
        /// <remarks>
        /// Applies to <see cref="DateTime"/> and nullable <see cref="DateTime"/> properties whose value is null
        /// or less than or equal to 1753/01/01 (the minimum SQL Server datetime).
        /// Returns <paramref name="data"/> unchanged when it is null. Read-only properties and indexers are skipped.
        /// </remarks>
        public static T NullDateTimeHandler<T>(this T data, DateTime? defaultTime = null)
        {
            if (data is null)
                return data;

            defaultTime ??= new DateTime(1753, 01, 01);

            foreach (PropertyInfo pi in GetCachedProperties(data.GetType()))
            {
                if (pi.PropertyType != typeof(DateTime) && pi.PropertyType != typeof(DateTime?))
                    continue;

                var dateTime = (DateTime?)pi.GetValue(data, null);

                // min date accept is 1753/01/01
                if (dateTime == null || dateTime <= new DateTime(1753, 01, 01))
                {
                    pi.SetValue(data, defaultTime);
                }
            }

            return data;
        }

        /// <summary>
        /// Change null string value to string.Empty in object
        /// </summary>
        /// <remarks>
        /// Returns <paramref name="data"/> unchanged when it is null. Read-only properties and indexers are skipped.
        /// </remarks>
        public static T NullStringHandler<T>(this T data)
        {
            if (data is null)
                return data;

            foreach (PropertyInfo pi in GetCachedProperties(data.GetType()))
            {
                if (pi.PropertyType == typeof(string) && string.IsNullOrEmpty((string?)pi.GetValue(data, null)))
                {
                    pi.SetValue(data, string.Empty);
                }
            }

            return data;
        }
    }
}
