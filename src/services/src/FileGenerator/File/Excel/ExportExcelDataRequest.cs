using System.Data;

namespace Light.File.Excel
{
    public class ExportExcelDataRequest
    {
        public DataTable Rows { get; set; } = null!;

        public string SheetName { get; set; } = null!;
    }
}
