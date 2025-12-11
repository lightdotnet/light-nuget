using Light.Extensions.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Light.Extensions
{
    public static class ArgumentChecker
    {

        /// <summary>
        /// Throw an ArgumentNullException when string is null or empty or white space
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="paramName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string ThrowIfNull(
            [NotNull][ValidatedNotNull] this string input,
            string? paramName = null,
            string? message = null)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input))
            {
                if (string.IsNullOrEmpty(message))
                {
                    message = $"String {paramName} is null or empty or whitespace";
                }

                throw new ArgumentNullException(paramName, message);
            }

            return input;
        }

        /// <summary>
        /// Throw an ArgumentNullException when input is null or default
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="paramName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static T ThrowIfNull<T>(
            [NotNull][ValidatedNotNull] this T input,
            string? paramName = null,
            string? message = null)
        {
            if (input is null || EqualityComparer<T>.Default.Equals(input))
            {
                if (string.IsNullOrEmpty(paramName))
                {
                    paramName ??= typeof(T).Name;
                }

                if (string.IsNullOrEmpty(message))
                {
                    message = $"{paramName} is null or default";
                }

                throw new ArgumentNullException(paramName, message);
            }

            return input;
        }
    }
}