using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace Light.Extensions
{
    public class EnumData
    {
        public int Value { get; internal set; }
        public string StringValue { get; internal set; } = default!;
        public string Description { get; internal set; } = default!;
    }

    public static class EnumHelper
    {
        private static string RegexReplace(string value)
        {
            value = Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");
            value = Regex.Replace(value, "([A-Za-z])([0-9])", "$1 $2");
            value = Regex.Replace(value, "([0-9])([A-Za-z])", "$1 $2");
            value = Regex.Replace(value, "(?<!^)(?<! )([A-Z][a-z])", " $1");

            return value;
        }

        private static string DefaultName(this Enum enumValue)
        {
            string result = enumValue.ToString();
            return RegexReplace(result);
        }

        /// <summary>
        /// Get description attribute value of Enum
        /// </summary>
        public static string? GetDescription(this Enum enumValue)
        {
            object[] attr = enumValue.GetType().GetField(enumValue.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attr.Length > 0)
                return ((DescriptionAttribute)attr[0]).Description;

            return default;
        }

        /// <summary>
        /// Get name value from display attribute of Enum
        /// </summary>

        public static string? GetNameOfDisplay(this Enum enumValue)
        {
            object[] attr = enumValue.GetType().GetField(enumValue.ToString())
                .GetCustomAttributes(typeof(DisplayAttribute), false);

            if (attr.Length > 0)
            {
                var value = ((DisplayAttribute)attr[0]).Name;

                if (!string.IsNullOrEmpty(value))
                    return value;
            }

            return default;
        }

        /// <summary>
        /// Get description value from display attribute of Enum
        /// </summary>
        public static string? GetDescriptionOfDisplay(this Enum enumValue)
        {
            object[] attr = enumValue.GetType().GetField(enumValue.ToString())
                .GetCustomAttributes(typeof(DisplayAttribute), false);

            if (attr.Length > 0)
            {
                var value = ((DisplayAttribute)attr[0]).Description;

                if (!string.IsNullOrEmpty(value))
                    return value;
            }

            return default;
        }

        /// <summary>
        /// Get enum values & descriptions
        /// </summary>
        public static IEnumerable<EnumData> GetOptions<T>()
            where T : Enum
        {
            Type enumType = typeof(T);

            if (enumType.BaseType != typeof(Enum))
                throw new ArgumentException("T is not System.Enum");

            List<EnumData> enumValList = new List<EnumData>();

            foreach (var e in Enum.GetValues(enumType))
            {
                var fi = e.GetType().GetField(e.ToString());
                var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

                var data = new EnumData
                {
                    Value = (int)e,
                    StringValue = e.ToString(),
                    Description = attributes.Length > 0 ? attributes[0].Description : "",
                };

                enumValList.Add(data);
            }

            return enumValList;
        }

        public static IEnumerable<T> GetAll<T>()
            where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }
    }
}
