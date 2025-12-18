using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Light.Extensions
{
    public static class TextHelper
    {
        /// <summary>
        /// Convert a string to an unsigned string (remove all accents)
        ///     like "Tiếng Việt" to "Tieng Viet"
        /// </summary>
        public static string ConvertToUnSign3(string s)
        {
            var regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = s.Normalize(NormalizationForm.FormD);
            return regex.Replace(temp, String.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');
        }
    }
}
