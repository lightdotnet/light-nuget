using DocumentFormat.OpenXml.Spreadsheet;
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

            Stream stream = _excelService.Export((list, null));
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

        [HttpPost("upload")]
        public IActionResult Import([FromBody] byte[] bytes)
        {
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

        [HttpGet("export_multi_list")]
        public IActionResult ExportMultiList()
        {
            var list1 = new List<object>
            {
                new { Id = 1, Name = "1" },
                new { Id = 2, Name = "2" }
            };

            var list2 = new List<object>
            {
                new { Code = 3, Desc = "3", List = new List<string> {"3", "4" } },
                new { Code = 4, Desc = "4", List = new List<string> {"3", "4" } }
            };

            var obj3 = new
            {
                Pro = "Pro 1",
                Detail = "Detail 1"
            };

            var sheets = new (object Data, string? Name)[]
            {
                new (list1, null),
                new (list2, null),
                new (obj3, null),
            };

            Stream stream = _excelService.Export(sheets);
            return File(stream, "application/octet-stream", "DataExport.xlsx"); // returns a FileStreamResult
        }

        [HttpGet("export_multi_dt")]
        public IActionResult ExportMultiDatatable()
        {
            DataTable dt = new DataTable();
            dt.Clear();
            dt.Columns.Add("Id");
            dt.Columns.Add("Name");
            DataRow row = dt.NewRow();
            row["Id"] = "1";
            row["Name"] = "test";
            dt.Rows.Add(row);

            DataTable dt2 = new DataTable();
            dt2.Clear();
            dt2.Columns.Add("Id");
            dt2.Columns.Add("Name");
            DataRow row2 = dt.NewRow();
            row2["Id"] = "1";
            row2["Name"] = "test";
            dt2.Rows.Add(row2);

            Stream stream = _excelService.Export((dt, null), (dt2, null));
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
