using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Light.Infrastructure.Excel
{
    internal static class Extensions
    {
        internal static Stream AsStream(this XLWorkbook workbook)
        {
            Stream stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        internal static IXLWorksheet GetWorksheet(IXLWorkbook workbook, string? sheetName) =>
            !string.IsNullOrEmpty(sheetName)
                ? workbook.Worksheet(sheetName)
                : workbook.Worksheet(1);

        /// <summary>
        /// Non-blank header cells of the first row, paired with their actual (1-based) column number,
        /// so that blank header cells don't shift the column mapping.
        /// </summary>
        internal static List<(string Name, int Column)> GetHeaders(IXLWorksheet worksheet) =>
            worksheet.FirstRow()
                     .CellsUsed()
                     .Select(c => (Name: c.Value.ToString(), Column: c.Address.ColumnNumber))
                     .ToList();

        /// <summary>
        /// Loose value of a cell: integral numbers as <see cref="long"/>, other numbers as <see cref="double"/>,
        /// dates as <see cref="DateTime"/>, booleans as <see cref="bool"/>, everything else as string.
        /// </summary>
        internal static object GetLooseValue(IXLCell cell)
        {
            var value = cell.Value;

            switch (value.Type)
            {
                case XLDataType.Number:
                    var number = value.GetNumber();
                    return IsIntegral(number) ? (object)(long)number : number;
                case XLDataType.DateTime:
                    return value.GetDateTime();
                case XLDataType.Boolean:
                    return value.GetBoolean();
                default:
                    return value.ToString();
            }
        }

        /// <summary>
        /// Converts a cell to <paramref name="type"/> using the cell's typed value and <see cref="CultureInfo.InvariantCulture"/>.
        /// Returns false when the cell is blank and the target is a non-nullable value type (the property should be left untouched).
        /// </summary>
        internal static bool TryConvertCell(IXLCell cell, Type type, out object? result)
        {
            result = null;

            var underlying = Nullable.GetUnderlyingType(type);
            var isNullable = underlying != null || !type.IsValueType;
            var target = underlying ?? type;
            var value = cell.Value;

            if (target == typeof(string))
            {
                result = cell.GetString();
                return true;
            }

            if (value.IsBlank || (value.IsText && string.IsNullOrWhiteSpace(value.GetText())))
            {
                return isNullable;
            }

            switch (value.Type)
            {
                case XLDataType.Number:
                    var number = value.GetNumber();
                    if ((IsIntegralType(target) || target.IsEnum) && Math.Floor(number) != number)
                    {
                        throw new FormatException(
                            $"Cell {cell.Address} contains the fractional number {number.ToString(CultureInfo.InvariantCulture)} which cannot be converted to {target.Name} without losing data.");
                    }
                    result = ConvertNumber(number, target);
                    return true;
                case XLDataType.DateTime:
                    result = ConvertDateTime(value.GetDateTime(), target);
                    return true;
                case XLDataType.Boolean:
                    result = target.IsEnum
                        ? Enum.ToObject(target, value.GetBoolean() ? 1 : 0)
                        : Convert.ChangeType(value.GetBoolean(), target, CultureInfo.InvariantCulture);
                    return true;
                case XLDataType.TimeSpan when target == typeof(TimeSpan):
                    result = value.GetTimeSpan();
                    return true;
                default:
                    result = ConvertText(value.IsText ? value.GetText() : value.ToString(), target);
                    return true;
            }
        }

        private static bool IsIntegralType(Type type) =>
            type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)
            || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte);

        private static bool IsIntegral(double number) =>
            Math.Floor(number) == number && number >= long.MinValue && number <= long.MaxValue;

        private static object ConvertNumber(double number, Type target)
        {
            if (target == typeof(double)) return number;
            if (target == typeof(decimal)) return (decimal)number;
            if (target == typeof(float)) return (float)number;
            if (target == typeof(DateTime)) return DateTime.FromOADate(number);
            if (target == typeof(DateTimeOffset)) return new DateTimeOffset(DateTime.FromOADate(number));
            if (target == typeof(TimeSpan)) return TimeSpan.FromDays(number);
            if (target == typeof(object)) return IsIntegral(number) ? (object)(long)number : number;
            if (target.IsEnum) return Enum.ToObject(target, (long)number);

            // integral targets (int, long, short, byte, ...) and bool; fractional values for integral targets are rejected earlier
            return Convert.ChangeType(number, target, CultureInfo.InvariantCulture);
        }

        private static object ConvertDateTime(DateTime dateTime, Type target)
        {
            if (target == typeof(DateTime) || target == typeof(object)) return dateTime;
            if (target == typeof(DateTimeOffset)) return new DateTimeOffset(dateTime);
            if (target == typeof(double)) return dateTime.ToOADate();

            return Convert.ChangeType(dateTime, target, CultureInfo.InvariantCulture);
        }

        private static object ConvertText(string text, Type target)
        {
            text = text.Trim();

            if (target == typeof(object)) return text;
            if (target.IsEnum) return Enum.Parse(target, text);
            if (target == typeof(Guid)) return Guid.Parse(text);
            if (target == typeof(DateTime)) return DateTime.Parse(text, CultureInfo.InvariantCulture);
            if (target == typeof(DateTimeOffset)) return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture);
            if (target == typeof(TimeSpan)) return TimeSpan.Parse(text, CultureInfo.InvariantCulture);

            return Convert.ChangeType(text, target, CultureInfo.InvariantCulture);
        }
    }
}
