using Light.Extensions;
using Light.File.Excel;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace WebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ExcelController(IExcelService excelService) : ControllerBase
    {
        private readonly IExcelService _excelService = excelService;

        [HttpGet]
        public IActionResult Export()
        {
            var list = new List<object>
            {
                new
                {
                    Id = 1,
                    Name = "Product 1",
                    Description = "Description for Product 1"
                },
                new
                {
                    Id = 2,
                    Name = "Product 2",
                    Description = "Description for Product 2"
                },
                new
                {
                    Id = 3,
                    Name = "Product 3",
                    Description = "Description for Product 3"
                }
            };

            Stream stream = _excelService.Export(list);
            return File(stream, "application/octet-stream", "DataExport.xlsx"); // returns a FileStreamResult
        }

        [HttpGet("import")]
        public IActionResult Import()
        {
            var filePath = Path.Combine(@"D:\\", "test.xlsx");

            //Read the File data into Byte Array.
            byte[] bytes = System.IO.File.ReadAllBytes(filePath);

            Stream stream = new MemoryStream(bytes);

            var dt = _excelService.ReadAsDataTable(stream);

            var obj = DataTableHelper.ConvertToObjects(dt);

            return Ok(obj);
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            int[] list = [1, 2, 3, 4];

            foreach (var item in list.Select((value, i) => new { i, value }))
            {
                var value = item.value;
                var index = item.i;
                Console.WriteLine($"[{item.i}] - {item.value}");
            }

            return Ok();
        }

        [HttpGet("export_dt")]
        public IActionResult ExportDatatable()
        {
            DataTable dt = new DataTable();
            dt.Clear();
            dt.Columns.Add("Id");
            dt.Columns.Add("Name");
            DataRow row = dt.NewRow();
            row["Id"] = "1";
            row["Name"] = "test";
            dt.Rows.Add(row);

            Stream stream = _excelService.Export(dt);
            return File(stream, "application/octet-stream", "DataExport.xlsx"); // returns a FileStreamResult
        }
    }
}

public enum ImportType
{
    None,
    New,
    Update,
    Delete
}

public class ImportTemp
{
    public int Id { get; set; }
    public ImportType Type { get; set; }
}
