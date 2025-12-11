using Light.Extensions;
using System;
using System.Linq;

namespace Light.Extensions
{
    public static class RandomHelper
    {
        /// <summary>
        ///     Generate a random string from characters library
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private static string Generate(string characterLibs, int length)
        {
            Random random = new Random();
            return new string(Enumerable.Repeat(characterLibs, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        ///     Generate a random string
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string String(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return Generate(chars, length);
        }

        /// <summary>
        ///     Generate a random number
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string Number(int length)
        {
            const string chars = "0123456789";
            return Generate(chars, length);
        }
    }
}

