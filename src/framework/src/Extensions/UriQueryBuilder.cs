using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Light.Extensions
{
    public class UriQueryBuilder
    {
        /// <summary>
        /// Build URL query for get data from an object
        /// </summary>
        public static string ToQueryString<T>(T data)
        {
            var properties = from p in data?.GetType().GetProperties()
                             where p.GetValue(data, null) != null
                             select p.Name + "=" + HttpUtility.UrlEncode(p.GetValue(data, null).ToString());

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
        public static string ToQueryString(Dictionary<string, object> queryParams)
        {
            var array = new List<string>();
            foreach (var pair in queryParams)
            {
                array.Add($"{HttpUtility.UrlEncode(pair.Key)}={HttpUtility.UrlEncode(pair.Value.ToString())}");
            }

            return string.Join("&", array);
        }
    }
}
