using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System;
using System.Globalization;

namespace Light.Infrastructure.Csv
{
    public class ObjectConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrEmpty(text)) return null;

            var culture = CultureInfo.InvariantCulture;

            // Try to parse as long, decimal (exact fractional values), double (exponent/out-of-range), bool, DateTime, or return as string
            if (long.TryParse(text, NumberStyles.Integer, culture, out long intValue)) return intValue;
            if (decimal.TryParse(text, NumberStyles.Number, culture, out decimal decimalValue)) return decimalValue;
            if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, culture, out double doubleValue)) return doubleValue;
            if (bool.TryParse(text, out bool boolValue)) return boolValue;
            if (DateTime.TryParse(text, culture, DateTimeStyles.None, out DateTime dateTimeValue)) return dateTimeValue;

            return text; // Return as string if no match
        }
    }
}
