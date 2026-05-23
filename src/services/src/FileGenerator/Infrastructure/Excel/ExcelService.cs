using ClosedXML.Excel;
using Light.File.Excel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Light.Infrastructure.Excel
{
    public class ExcelService : IExcelService
    {
        public Stream Export(params (object Data, string? SheetName)[] sheets)
        {
            using var wb = new XLWorkbook();

            for (int i = 0; i < sheets.Length; i++)
            {
                var dataSheet = sheets[i].Data is IEnumerable
                    ? sheets[i].Data
                    : new[] { sheets[i].Data };

                wb.Worksheets
                    .Add(sheets[i].SheetName ?? $"sheet{i + 1}")
                    .FirstCell()
                    .InsertTable((dynamic)dataSheet, true);
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
            headers.ForEach(h => dt.Columns.Add(h));

            // Loop through the Worksheet rows, skip first row which is used for column header texts
            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var newRow = dt.NewRow();
                for (int i = 0; i < headers.Count; i++)
                    newRow[i] = row.Cell(i + 1).Value.ToString();
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

            // header column texts, indexing of ClosedXml is 1 not 0
            var headers = Extensions.GetHeaders(worksheet)
                .Select((name, i) => new { Name = name, Index = i + 1 })
                .ToList();

            var props = typeof(T).GetProperties();

            // skip first row which is used for column header texts
            return worksheet.RowsUsed().Skip(1).Select(row =>
            {
                var obj = (T)Activator.CreateInstance(typeof(T))!;

                foreach (var prop in props)
                {
                    // find column has same prop name of class
                    var propName = options?.ColumnNames.GetValueOrDefault(prop.Name) ?? prop.Name;
                    var column = headers.SingleOrDefault(c => c.Name == propName);
                    if (column is null) continue;

                    var val = row.Cell(column.Index).GetString(); // must .GetString() to fix error "object must implement IConvertible"
                    prop.SetValue(obj, Extensions.ConvertValue(val, prop.PropertyType));
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

                for (int i = 0; i < headers.Count; i++)
                {
                    var cell = row.Cell(i + 1); // because ClosedXML start with 1

                    // convert prop value to correct type
                    dict[headers[i]] = cell.Value switch
                    {
                        { IsNumber: true } => Convert.ToInt64(cell.Value.ToString()),
                        { IsDateTime: true } => Convert.ToDateTime(cell.Value.ToString()),
                        { IsBoolean: true } => Convert.ToBoolean(cell.Value.ToString()),
                        _ => cell.Value.ToString()
                    };
                }

                return dict;
            }).ToList();
        }
    }
}