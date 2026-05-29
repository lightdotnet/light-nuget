using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Light.File.Excel
{
    public static class ExcelExtensions
    {
        public static Stream Export<T>(this IExcelService excelService, IEnumerable<T> list, string? sheetName = null)
        {
            return excelService.Export((list, sheetName));
        }

        public static Stream Export(this IExcelService excelService, params Worksheet[] worksheets)
        {
            var sheets = worksheets.Select(s => (s.Data, s.SheetName)).ToArray();
            return excelService.Export(sheets);
        }
    }
}
