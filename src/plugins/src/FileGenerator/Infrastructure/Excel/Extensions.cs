using ClosedXML.Excel;
using System;
using System.Collections.Generic;
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

        internal static List<string> GetHeaders(IXLWorksheet worksheet) =>
            worksheet.FirstRow()
                     .CellsUsed()
                     .Select(c => c.Value.ToString())
                     .ToList();

        internal static object? ConvertValue(string val, Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (string.IsNullOrEmpty(val)) return null;
                return Convert.ChangeType(val, type.GetGenericArguments()[0]);
            }

            if (type.IsEnum)
                return Enum.Parse(type, val);

            return Convert.ChangeType(val, type);
        }
    }
}
