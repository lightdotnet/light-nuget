using ClosedXML.Excel;
using Light.File.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Light.Infrastructure.Excel
{
    public class ExcelService : IExcelService
    {
        public Stream Export(DataTable dataTable, string? sheetName = null)
        {
            using var wb = new XLWorkbook();
            wb.Worksheets.Add(dataTable, sheetName ?? "data");

            foreach (var ws in wb.Worksheets)
            {
                ws.ColumnsUsed().AdjustToContents(); // fit columns width
            }

            return wb.AsStream();
        }

        public Stream Export<T>(IEnumerable<T> data, string? sheetName = null)
        {
            using var wb = new XLWorkbook();
            wb.Worksheets.Add(sheetName ?? "data").FirstCell().InsertTable(data, true);

            foreach (var ws in wb.Worksheets)
            {
                ws.ColumnsUsed().AdjustToContents(); // fit columns width
            }

            return wb.AsStream();
        }

        public Stream Export(ExportExcelDataRequest[] request)
        {
            using var wb = new XLWorkbook();

            foreach (var obj in request)
            {
                wb.Worksheets.Add(obj.Rows, obj.SheetName);
            }

            foreach (var ws in wb.Worksheets)
            {
                ws.ColumnsUsed().AdjustToContents(); // fit columns width
            }

            return wb.AsStream();
        }

        public DataTable ReadAsDataTable(Stream streamData, string? sheetName = null)
        {
            using IXLWorkbook workbook = new XLWorkbook(streamData);

            // set worksheet
            var worksheet = !string.IsNullOrEmpty(sheetName)
                ? workbook.Worksheet(sheetName)
                : workbook.Worksheet(1);

            // read rows with index
            var rowsWithIndex = worksheet.Rows().Select((data, i) => new { i, data });

            // Create a new DataTable.
            var dt = new DataTable();
            int columnsCount = 0;

            // Loop through the Worksheet rows.
            foreach (var row in rowsWithIndex) // Skip first row which is used for column header texts
            {
                IXLRow rowData = row.data;
                int rowIndex = row.i;

                // Use the first row to add columns to DataTable.
                if (rowIndex == 0)
                {
                    foreach (IXLCell cell in rowData.Cells())
                    {
                        dt.Columns.Add(cell.Value.ToString());
                    }

                    columnsCount = dt.Columns.Count;
                }
                else
                {
                    // Add rows to DataTable.
                    dt.Rows.Add();

                    for (var i = 0; i < columnsCount; i++)
                    {
                        var cell = rowData.Cell(i + 1); // because ClosedXML start with 1

                        dt.Rows[^1][i] = cell.Value.ToString();
                    }
                }
            }

            return dt;
        }

        public IEnumerable<T> ReadAs<T>(Stream streamData, string? sheetName = null, ColumnOptions<T>? options = null)
        {
            var list = new List<T>();

            Type typeOfObject = typeof(T);

            using (IXLWorkbook workbook = new XLWorkbook(streamData))
            {
                // check sheet exists
                if (!string.IsNullOrEmpty(sheetName) && !workbook.Worksheets.Contains(sheetName))
                {
                    return list;
                }

                // get first sheet if not specify sheet name
                var worksheet = !string.IsNullOrEmpty(sheetName)
                    ? workbook.Worksheet(sheetName)
                    : workbook.Worksheet(1);

                var properties = typeOfObject.GetProperties();

                // header column texts
                var columns = worksheet.FirstRow().Cells().Select((v, i) =>
                    new { v.Value, Index = i + 1 }); // indexing of ClosedXml is 1 not 0

                foreach (IXLRow row in worksheet.RowsUsed().Skip(1))// Skip first row which is used for column header texts
                {
                    T obj = (T)Activator.CreateInstance(typeOfObject)!;

                    foreach (var prop in properties)
                    {
                        // find column has same prop name of class
                        var propName = prop.Name.ToString();
                        if (options != null && options.ColumnNames.ContainsKey(propName))
                        {
                            propName = options.ColumnNames[propName];
                        }

                        var columnHasName = columns.SingleOrDefault(c => c.Value.ToString() == propName);
                        if (columnHasName != null)
                        {
                            int colIndex = columnHasName.Index;
                            var type = prop.PropertyType;
                            var val = row.Cell(colIndex).GetString(); // must .ToString() to fix error "object must implement Iconvertible"
                            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                if (string.IsNullOrEmpty(val))
                                    prop.SetValue(obj, null);
                                else
                                    prop.SetValue(obj, Convert.ChangeType(val, type.GetGenericArguments()[0]));
                            }
                            else if (prop.PropertyType.IsEnum)
                            {
                                var enumValue = Enum.Parse(prop.PropertyType, val);

                                prop.SetValue(obj, enumValue);
                            }
                            else
                            {
                                prop.SetValue(obj, Convert.ChangeType(val, type));
                            }
                        }
                    }

                    list.Add(obj);
                }
            }

            return list;
        }

        public List<Dictionary<string, object>> ReadAsObjects(Stream streamData, string? sheetName = null)
        {
            using IXLWorkbook workbook = new XLWorkbook(streamData);

            // set worksheet
            var worksheet = !string.IsNullOrEmpty(sheetName)
                ? workbook.Worksheet(sheetName)
                : workbook.Worksheet(1);

            // read rows with index
            var rowsWithIndex = worksheet.Rows().Select((data, i) => new { i, data });

            // new objects list with prop name is dictionary key and prop value is dictionary value
            var objects = new List<Dictionary<string, object>>();

            // hold columns name in excel file for define prop names
            var propNamesInObject = new List<string>();

            // Loop through the Worksheet rows.
            foreach (var row in rowsWithIndex) // Skip first row which is used for column header texts
            {
                IXLRow rowData = row.data;
                int rowIndex = row.i;

                // Use first row for define prop names in object.
                if (rowIndex == 0)
                {
                    propNamesInObject = rowData.Cells().Select(s => s.Value.ToString()).ToList();
                }
                else
                {
                    var obj = new Dictionary<string, object>();

                    for (var i = 0; i < propNamesInObject.Count; i++)
                    {
                        var cell = rowData.Cell(i + 1);
                        var propName = propNamesInObject[i];
                        var propValue = cell.Value.ToString();

                        // convert prop value to correct type
                        if (cell.Value.IsNumber)
                        {
                            obj[propName] = Convert.ToInt64(propValue);
                        }
                        else if (cell.Value.IsDateTime)
                        {
                            obj[propName] = Convert.ToDateTime(propValue);
                        }
                        else if (cell.Value.IsBoolean)
                        {
                            obj[propName] = Convert.ToBoolean(propValue);
                        }
                        else
                        {
                            obj[propName] = propValue;
                        }
                    }

                    objects.Add(obj);
                }
            }

            return objects;
        }
    }
}