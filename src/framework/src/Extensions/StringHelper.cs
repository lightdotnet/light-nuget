using System;

namespace Light.Extensions
{
    public static class StringHelper
    {
        /// <summary>
        ///     Get substring of specified number of characters on the left.
        /// </summary>
        public static string Left(this string value, int length)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            length = Math.Abs(length);

            return value.Length <= length
                   ? value
                   : value[..length];
        }

        /// <summary>
        ///     Get substring of specified number of characters on the right.
        /// </summary>
        public static string Right(this string value, int length)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            length = Math.Abs(length);

            return value.Length <= length
                   ? value
                   : value[(value.Length - length)..];
        }

        /// <summary>
        ///     Get characters from left to character input
        /// </summary>
        public static string Left(this string value, string c)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // get poisition of char in value
            int idx = value.IndexOf(c);

            if (idx < 0) // < 0 is not contain char in value
                return value;

            return value[..idx]; //value.Substring(0, idx);
        }

        /// <summary>
        ///     Get characters from right to last character input
        /// </summary>
        public static string Right(this string value, string c)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // get poisition of char in value
            int idx = value.LastIndexOf(c);

            if (idx < 0) // < 0 is not contain char in value
                return value;

            return value[(idx + 1)..];
        }
    }
}
