using System.Collections.Generic;
using System.Data;
using System.IO;

namespace Light.File.Excel
{
    public interface IExcelService
    {
        /// <summary>
        /// Export a DataTable to file
        /// </summary>
        Stream Export(DataTable dataTable, string? sheetName = null);

        /// <summary>
        /// Export a list data to file
        /// </summary>
        Stream Export<T>(IEnumerable<T> data, string? sheetName = null);

        /// <summary>
        /// Export multi list data to file
        /// </summary>
        Stream Export(ExportExcelDataRequest[] request);

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
