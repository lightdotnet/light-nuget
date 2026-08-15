using System;
using System.Linq;

namespace Light.Extensions
{
    public static class RandomHelper
    {
        // netstandard2.1 has no Random.Shared (added in .NET 6); share one instance under a lock instead
        // of allocating a new, time-seeded Random per call (which can produce identical sequences when
        // called in quick succession).
        private static readonly Random SharedRandom = new Random();
        private static readonly object SyncRoot = new object();

        /// <summary>
        ///     Generate a random string from characters library
        /// </summary>
        /// <param name="characterLibs"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private static string Generate(string characterLibs, int length)
        {
            lock (SyncRoot)
            {
                return new string(Enumerable.Repeat(characterLibs, length).Select(s => s[SharedRandom.Next(s.Length)]).ToArray());
            }
        }

        /// <summary>
        ///     Generate a random string
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string GenerateString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return Generate(chars, length);
        }

        /// <summary>
        ///     Generate a random number
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string GenerateNumber(int length)
        {
            const string chars = "0123456789";
            return Generate(chars, length);
        }
    }
}

