using System;
using System.IO;

namespace Light.Extensions
{
    public static class StreamHelper
    {
        /// <summary>
        /// Convert Stream to Base64 string
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string ToBase64String(this Stream stream)
        {
            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }

            string base64 = Convert.ToBase64String(bytes);

            return base64;
        }

        /// <summary>
        /// Read Base64 string to MemoryStream
        /// </summary>
        /// <param name="base64String"></param>
        /// <returns></returns>
        public static MemoryStream FromBase64String(string base64String)
        {
            var bytes = Convert.FromBase64String(base64String);

            var stream = new MemoryStream(bytes);

            return stream;
        }
    }
}
