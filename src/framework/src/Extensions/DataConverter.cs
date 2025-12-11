using System;
using System.Collections.Generic;
using System.Linq;

namespace Light.Extensions
{
    public static class DataConverter
    {
        private const string _defaultSplitChar = "|";

        /// <summary>
        /// Split string to array
        /// </summary>
        public static string[] ToArray(this string? value, string splitChar = _defaultSplitChar)
        {
            if (string.IsNullOrEmpty(value))
                return Array.Empty<string>();

            return value.Split(new string[] { splitChar }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Split string to list
        /// </summary>
        public static IEnumerable<string> ToList(this string? value, string splitChar = _defaultSplitChar)
        {
            if (string.IsNullOrEmpty(value))
                return new List<string>();

            return value.Split(new string[] { splitChar }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Join a List, Array of string to string
        /// </summary>
        public static string JoinToString(this IList<string> values, string splitChar = _defaultSplitChar)
        {
            //check list name extentions
            return values != null && values.Any()
                ? string.Join(splitChar, values)
                : string.Empty;
        }

        /// <summary>
        /// Join a IEnumerable of string to string
        /// </summary>
        public static string JoinToString(this IEnumerable<string> values, string splitChar = _defaultSplitChar)
        {
            //check list name extentions
            return values != null && values.Any()
                ? string.Join(splitChar, values)
                : string.Empty;
        }

        /// <summary>
        /// Convert AreaText HTML to array
        /// </summary>
        public static string[] AreaTextToArray(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return Array.Empty<string>();

            return value.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Convert AreaText HTML to a list string
        /// </summary>
        public static List<string> AreaTextToList(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return new List<string>();

            return new List<string>(value.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Convert number to binary (2, 8, 10, 16)
        /// </summary>
        public static string ToBase(this long value, int toBase)
        {
            return Convert.ToString(value, toBase);
        }

        /// <summary>
        /// Convert number to base 2
        /// </summary>
        public static string ToBase2(this long value)
        {
            return Convert.ToString(value, 2);
        }

        /// <summary>
        /// Convert number to base 8
        /// </summary>
        public static string ToBase8(this long value)
        {
            return Convert.ToString(value, 8);
        }

        /// <summary>
        /// Convert number to base 10
        /// </summary>
        public static string ToBase10(this long value)
        {
            return Convert.ToString(value, 10);
        }

        /// <summary>
        /// Convert number to base 16
        /// </summary>
        public static string ToBase16(this long value)
        {
            return Convert.ToString(value, 16);
        }

        /// <summary>
        /// Convert binary (2, 8, 10, 16 default is 16) to number
        /// </summary>
        public static long ToInt64FromBinary(this string baseString, int toBase = 16)
        {
            if (string.IsNullOrEmpty(baseString))
                return 0;

            return Convert.ToInt64(baseString, toBase);
        }
    }
}