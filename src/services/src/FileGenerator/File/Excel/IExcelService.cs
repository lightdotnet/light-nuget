using System.Collections.Generic;
using System.Data;
using System.IO;

namespace Light.File.Excel
{
    public interface IExcelService
    {
        /// <summary>
        /// Export multi list data to file
        /// </summary>
        Stream Export(params Worksheet[] sheets);

        /// <summary>
        /// Load excel file to DataTable
        /// </summary>
        DataTable ReadAsDataTable(Stream streamData, string? sheetName = null);

        /// <summary>
        /// Load excel file to list object with manual set column names
        /// </summary>
        IEnumerable<T> ReadAs<T>(Stream streamData, string? sheetName = null, ColumnOptions<T>? options = null);

        /// <summary>
        /// Load excel file to objects list
        /// </summary>
        List<Dictionary<string, object>> ReadAsObjects(Stream streamData, string? sheetName = null);
    }
}
