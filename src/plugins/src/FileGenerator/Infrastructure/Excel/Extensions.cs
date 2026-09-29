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
        /// <exception cref="FormatException">The cell value can't be converted to the target type (the message names the cell).</exception>
        /// <exception cref="OverflowException">The cell value is outside the target type's range (the message names the cell).</exception>
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

            if (value.Type == XLDataType.Number && (IsIntegralType(target) || target.IsEnum))
            {
                var number = value.GetNumber();
                if (Math.Floor(number) != number)
                {
                    throw new FormatException(
                        $"Cell {cell.Address} contains the fractional number {number.ToString(CultureInfo.InvariantCulture)} which cannot be converted to {target.Name} without losing data.");
                }
            }

            try
            {
                result = ConvertValue(value, target);
                return true;
            }
            catch (OverflowException e)
            {
                throw new OverflowException(
                    $"Cell {cell.Address} value '{Display(value)}' is outside the range of {target.Name}.", e);
            }
            catch (Exception e) when (e is FormatException || e is InvalidCastException || e is ArgumentException)
            {
                throw new FormatException(
                    $"Cell {cell.Address} value '{Display(value)}' cannot be converted to {target.Name}.", e);
            }
        }

        private static object ConvertValue(XLCellValue value, Type target)
        {
            switch (value.Type)
            {
                case XLDataType.Number:
                    return ConvertNumber(value.GetNumber(), target);
                case XLDataType.DateTime:
                    return ConvertDateTime(value.GetDateTime(), target);
                case XLDataType.Boolean:
                    return target.IsEnum
                        ? Enum.ToObject(target, value.GetBoolean() ? 1 : 0)
                        : Convert.ChangeType(value.GetBoolean(), target, CultureInfo.InvariantCulture);
                case XLDataType.TimeSpan when target != typeof(object):
                    return ConvertTimeSpan(value.GetTimeSpan(), target);
                default:
                    // text, errors, and time cells into object (kept as text, like GetLooseValue)
                    return ConvertText(value.IsText ? value.GetText() : value.ToString(), target);
            }
        }

        private static string Display(XLCellValue value) =>
            value.Type == XLDataType.Number
                ? value.GetNumber().ToString("R", CultureInfo.InvariantCulture)
                : value.ToString();

        private static bool IsIntegralType(Type type) =>
            type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)
            || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte);

        private static bool IsIntegral(double number) =>
            Math.Floor(number) == number && number >= long.MinValue && number <= long.MaxValue;

        /// <summary>
        /// Excel dates carry no time zone: the date/time is kept as-is and given the machine's local offset
        /// (<c>new DateTimeOffset(dateTime)</c> with <see cref="DateTimeKind.Unspecified"/>), as before.
        /// </summary>
        private static DateTimeOffset ToDateTimeOffset(DateTime dateTime) =>
            new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified));

        private static object ConvertNumber(double number, Type target)
        {
            if (target == typeof(double)) return number;
            if (target == typeof(decimal)) return (decimal)number;
            if (target == typeof(float)) return (float)number;
            if (target == typeof(DateTime)) return DateTime.FromOADate(number);
            if (target == typeof(DateTimeOffset)) return ToDateTimeOffset(DateTime.FromOADate(number));
            if (target == typeof(TimeSpan)) return TimeSpan.FromDays(number);
            if (target == typeof(object)) return IsIntegral(number) ? (object)(long)number : number;

            // range-checked through the enum's underlying type (Convert throws OverflowException)
            if (target.IsEnum) return Enum.ToObject(target, Convert.ChangeType(number, Enum.GetUnderlyingType(target), CultureInfo.InvariantCulture));

            // integral targets (int, long, short, byte, ...) and bool; fractional values for integral targets are rejected earlier
            return Convert.ChangeType(number, target, CultureInfo.InvariantCulture);
        }

        private static object ConvertDateTime(DateTime dateTime, Type target)
        {
            if (target == typeof(DateTime) || target == typeof(object)) return dateTime;
            if (target == typeof(DateTimeOffset)) return ToDateTimeOffset(dateTime);
            if (target == typeof(double)) return dateTime.ToOADate();

            return Convert.ChangeType(dateTime, target, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Time cells: <see cref="TimeSpan"/> as-is, floating-point/decimal targets get Excel's serial value
        /// (fraction of a day); any other target is rejected.
        /// </summary>
        private static object ConvertTimeSpan(TimeSpan timeSpan, Type target)
        {
            if (target == typeof(TimeSpan)) return timeSpan;
            if (target == typeof(double)) return timeSpan.TotalDays;
            if (target == typeof(decimal)) return (decimal)timeSpan.TotalDays;
            if (target == typeof(float)) return (float)timeSpan.TotalDays;

            throw new InvalidCastException($"A time value cannot be converted to {target.Name}.");
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

            // explicit number styles (the same ones Convert.ChangeType uses), always invariant
            var culture = CultureInfo.InvariantCulture;
            if (target == typeof(int)) return int.Parse(text, NumberStyles.Integer, culture);
            if (target == typeof(long)) return long.Parse(text, NumberStyles.Integer, culture);
            if (target == typeof(short)) return short.Parse(text, NumberStyles.Integer, culture);
            if (target == typeof(byte)) return byte.Parse(text, NumberStyles.Integer, culture);
            if (target == typeof(uint)) return uint.Parse(text, NumberStyles.Integer, culture);
            if (target == typeof(ulong)) return ulong.Parse(text, NumberStyles.Integer, culture);
            if (target == typeof(ushort)) return ushort.Parse(text, NumberStyles.Integer, culture);
            if (target == typeof(sbyte)) return sbyte.Parse(text, NumberStyles.Integer, culture);
            if (target == typeof(decimal)) return decimal.Parse(text, NumberStyles.Number, culture);
            if (target == typeof(double)) return double.Parse(text, NumberStyles.Float | NumberStyles.AllowThousands, culture);
            if (target == typeof(float)) return float.Parse(text, NumberStyles.Float | NumberStyles.AllowThousands, culture);

            return Convert.ChangeType(text, target, culture);
        }
    }
}
