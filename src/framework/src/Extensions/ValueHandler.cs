using System;
using System.Linq;
using System.Reflection;

namespace Light.Extensions
{
    public static class ValueHandler
    {
        /// <summary>
        /// Trim limit lenght of string in object
        /// </summary>
        public static T MaximumCharHandler<T>(this T data, int lenght)
        {
            //select props string & value = null
            var nullValueProperties = data?.GetType().GetProperties()
                .Where(pi =>
                    pi.PropertyType == typeof(string) &&
                    !string.IsNullOrEmpty((string)pi.GetValue(data, null))
                );

            foreach (PropertyInfo pi in nullValueProperties!)
            {
                pi.SetValue(data, pi.GetValue(data, null).ToString().Left(lenght));
            }

            return data;
        }

        /// <summary>
        /// Change null Datetime values to default time in object
        /// </summary>
        public static T NullDateTimeHandler<T>(this T data, DateTime? defaultTime = null)
        {
            defaultTime ??= new DateTime(1753, 01, 01);

            var nullValueProperties = data?.GetType().GetProperties()
                .Where(pi => pi.PropertyType == typeof(DateTime));

            foreach (PropertyInfo pi in nullValueProperties!)
            {
                var dateTime = (DateTime)pi.GetValue(data, null);

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
        public static T NullStringHandler<T>(this T data)
        {
            //select props string & value = null
            var nullValueProperties = data?.GetType().GetProperties()
                .Where(pi =>
                    pi.PropertyType == typeof(string) &&
                    string.IsNullOrEmpty((string)pi.GetValue(data, null))
                );

            foreach (PropertyInfo pi in nullValueProperties!)
            {
                pi.SetValue(data, string.Empty);
            }

            return data;
        }
    }
}