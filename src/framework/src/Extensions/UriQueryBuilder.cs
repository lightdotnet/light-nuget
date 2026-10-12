using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace Light.Extensions
{
    public class UriQueryBuilder
    {
        /// <summary>
        /// Build URL query for get data from an object
        /// </summary>
        /// <remarks>
        /// Null properties and indexers are skipped. Names and values are URL-encoded;
        /// values are formatted with <see cref="CultureInfo.InvariantCulture"/>.
        /// </remarks>
        public static string ToQueryString<T>(T data)
        {
            if (data is null)
                return string.Empty;

            var properties = from p in data.GetType().GetProperties()
                             where p.CanRead && p.GetIndexParameters().Length == 0
                             let value = p.GetValue(data, null)
                             where value != null
                             select HttpUtility.UrlEncode(p.Name) + "=" + HttpUtility.UrlEncode(FormatValue(value));

            return string.Join("&", properties.ToArray());
        }

        /// <summary>
        /// Build URL query for get data from a Dictionary
        /// </summary>
        public static string ToQueryString(Dictionary<string, string> queryParams)
        {
            var array = new List<string>();
            foreach (var pair in queryParams)
            {
                array.Add($"{HttpUtility.UrlEncode(pair.Key)}={HttpUtility.UrlEncode(pair.Value)}");
            }

            return string.Join("&", array);
        }

        /// <summary>
        /// Build URL query for get data from a Dictionary
        /// </summary>
        /// <remarks>
        /// Null values are emitted as an empty value (<c>key=</c>), matching the string dictionary overload.
        /// Values are formatted with <see cref="CultureInfo.InvariantCulture"/>.
        /// </remarks>
        public static string ToQueryString(Dictionary<string, object> queryParams)
        {
            var array = new List<string>();
            foreach (var pair in queryParams)
            {
                array.Add($"{HttpUtility.UrlEncode(pair.Key)}={HttpUtility.UrlEncode(FormatValue(pair.Value))}");
            }

            return string.Join("&", array);
        }

        private static string? FormatValue(object? value) =>
            value is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : value?.ToString();
    }
}
