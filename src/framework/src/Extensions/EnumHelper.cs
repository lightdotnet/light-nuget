using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Light.Extensions
{
    public class EnumData
    {
        /// <summary>
        /// Numeric value of the enum member. For enums whose underlying type is wider than <see cref="int"/>
        /// (long, uint, ulong) values outside the <see cref="int"/> range are truncated; use <see cref="LongValue"/>.
        /// </summary>
        public int Value { get; internal set; }

        /// <summary>
        /// Numeric value of the enum member as <see cref="long"/> (ulong values above <see cref="long.MaxValue"/> wrap).
        /// </summary>
        public long LongValue { get; internal set; }

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

        // Undefined or combined [Flags] values have no matching field: return no attributes instead of throwing.
        private static object[] GetEnumAttributes(Enum enumValue, Type attributeType) =>
            enumValue.GetType().GetField(enumValue.ToString())?.GetCustomAttributes(attributeType, false)
            ?? Array.Empty<object>();

        private static long ToInt64(object enumValue) =>
            Type.GetTypeCode(Enum.GetUnderlyingType(enumValue.GetType())) == TypeCode.UInt64
                ? unchecked((long)Convert.ToUInt64(enumValue, CultureInfo.InvariantCulture))
                : Convert.ToInt64(enumValue, CultureInfo.InvariantCulture);

        /// <summary>
        /// Get description attribute value of Enum
        /// </summary>
        /// <remarks>Returns null for undefined or combined [Flags] values.</remarks>
        public static string? GetDescription(this Enum enumValue)
        {
            object[] attr = GetEnumAttributes(enumValue, typeof(DescriptionAttribute));

            if (attr.Length > 0)
                return ((DescriptionAttribute)attr[0]).Description;

            return default;
        }

        /// <summary>
        /// Get name value from display attribute of Enum
        /// </summary>
        /// <remarks>Returns null for undefined or combined [Flags] values.</remarks>
        public static string? GetNameOfDisplay(this Enum enumValue)
        {
            object[] attr = GetEnumAttributes(enumValue, typeof(DisplayAttribute));

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
        /// <remarks>Returns null for undefined or combined [Flags] values.</remarks>
        public static string? GetDescriptionOfDisplay(this Enum enumValue)
        {
            object[] attr = GetEnumAttributes(enumValue, typeof(DisplayAttribute));

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

            foreach (Enum e in Enum.GetValues(enumType))
            {
                var attributes = GetEnumAttributes(e, typeof(DescriptionAttribute)).OfType<DescriptionAttribute>().ToArray();
                var longValue = ToInt64(e);

                var data = new EnumData
                {
                    Value = unchecked((int)longValue),
                    LongValue = longValue,
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
