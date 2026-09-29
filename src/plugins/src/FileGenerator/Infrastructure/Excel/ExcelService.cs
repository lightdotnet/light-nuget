using ClosedXML.Excel;
using Light.File.Excel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Light.Infrastructure.Excel
{
    public class ExcelService : IExcelService
    {
        public Stream Export(params (object Data, string? SheetName)[] sheets)
        {
            using var wb = new XLWorkbook();

            for (int i = 0; i < sheets.Length; i++)
            {
                var (data, sheetName) = sheets[i];

                var ws = wb.Worksheets.Add(sheetName ?? $"sheet{i + 1}");

                if (data is DataTable dt)
                {
                    ws.FirstCell().InsertTable(dt, true);
                }
                else
                {
                    // string implements IEnumerable<char> but must be exported as a single value
                    var enumerable = data is IEnumerable && !(data is string)
                        ? data
                        : new object[] { data };

                    ws.FirstCell().InsertTable((dynamic)enumerable, true);
                }
            }

            foreach (var ws in wb.Worksheets)
                ws.ColumnsUsed().AdjustToContents(); // fit columns width

            return wb.AsStream();
        }

        public DataTable ReadAsDataTable(Stream streamData, string? sheetName = null)
        {
            using var workbook = new XLWorkbook(streamData);

            // set worksheet
            var worksheet = Extensions.GetWorksheet(workbook, sheetName);

            // header column texts
            var headers = Extensions.GetHeaders(worksheet);

            // Create a new DataTable
            var dt = new DataTable();
            headers.ForEach(h => dt.Columns.Add(h.Name));

            // Loop through the Worksheet rows, skip first row which is used for column header texts
            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var newRow = dt.NewRow();
                for (int i = 0; i < headers.Count; i++)
                    newRow[i] = row.Cell(headers[i].Column).Value.ToString();
                dt.Rows.Add(newRow);
            }

            return dt;
        }

        public IEnumerable<T> ReadAs<T>(Stream streamData, string? sheetName = null, ColumnOptions<T>? options = null)
        {
            using var workbook = new XLWorkbook(streamData);

            // check sheet exists
            if (!string.IsNullOrEmpty(sheetName) && !workbook.Worksheets.Contains(sheetName))
                return Enumerable.Empty<T>();

            // get first sheet if not specify sheet name
            var worksheet = Extensions.GetWorksheet(workbook, sheetName);

            // header column texts with their actual column number (indexing of ClosedXml is 1 not 0)
            var headers = Extensions.GetHeaders(worksheet);

            // build property -> column map once: only settable, non-indexer properties with a matching header
            // (the first matching header wins when a header text is duplicated)
            var columnMap = new List<(PropertyInfo Property, int Column)>();
            foreach (var prop in typeof(T).GetProperties())
            {
                if (!prop.CanWrite || prop.GetIndexParameters().Length > 0) continue;

                var propName = options?.ColumnNames.GetValueOrDefault(prop.Name) ?? prop.Name;
                var index = headers.FindIndex(c => c.Name == propName);
                if (index < 0) continue;

                columnMap.Add((prop, headers[index].Column));
            }

            // skip first row which is used for column header texts
            return worksheet.RowsUsed().Skip(1).Select(row =>
            {
                var obj = (T)Activator.CreateInstance(typeof(T))!;

                foreach (var (prop, column) in columnMap)
                {
                    // blank cells leave non-nullable value-type properties at their default
                    if (Extensions.TryConvertCell(row.Cell(column), prop.PropertyType, out var value))
                        prop.SetValue(obj, value);
                }

                return obj;
            }).ToList();
        }

        public List<Dictionary<string, object>> ReadAsObjects(Stream streamData, string? sheetName = null)
        {
            using var workbook = new XLWorkbook(streamData);

            // set worksheet
            var worksheet = Extensions.GetWorksheet(workbook, sheetName);

            // hold columns name in excel file for define prop names
            var headers = Extensions.GetHeaders(worksheet);

            // new objects list with prop name is dictionary key and prop value is dictionary value
            // skip first row which is used for column header texts
            return worksheet.RowsUsed().Skip(1).Select(row =>
            {
                var dict = new Dictionary<string, object>();

                foreach (var (name, column) in headers)
                {
                    // duplicated header text: the first column wins, same as ReadAs<T>
                    if (dict.ContainsKey(name)) continue;

                    // convert prop value to correct type from the cell's typed value (culture independent)
                    dict[name] = Extensions.GetLooseValue(row.Cell(column));
                }

                return dict;
            }).ToList();
        }
    }
}