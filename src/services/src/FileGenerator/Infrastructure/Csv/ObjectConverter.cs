using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System;

namespace Light.Infrastructure.Csv
{
    public class ObjectConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrEmpty(text)) return null;

            // Try to parse as int, double, bool, or return as string
            if (long.TryParse(text, out long intValue)) return intValue;
            if (double.TryParse(text, out double doubleValue)) return doubleValue;
            if (decimal.TryParse(text, out decimal decimalValue)) return decimalValue;
            if (bool.TryParse(text, out bool boolValue)) return boolValue;
            if (DateTime.TryParse(text, out DateTime dateTimeValue)) return dateTimeValue;

            return text; // Return as string if no match
        }
    }
}
