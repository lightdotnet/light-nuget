using CsvHelper.Configuration.Attributes;
using Light.File.Csv;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace WebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CsvController(ICsvService csvService) : ControllerBase
    {
        private readonly string _path = Path.Combine(@"D:\\", "Files", "Adobe_aswDM50210_20250311182339.csv");

        [HttpGet("read")]
        public IActionResult Get(string fileName)
        {
            var path = Path.Combine(@"D:\\", "Files", $"{fileName}.csv");

            var stream = new StreamReader(path);

            var dt = csvService.Read(stream);

            return Ok(dt);
        }

        [HttpGet("read_as")]
        public IActionResult ReadAs(string fileName)
        {
            var path = Path.Combine(@"D:\\", "Files", $"{fileName}.csv");

            var stream = new StreamReader(path);

            var dt = csvService.Read<CsvObject>(stream);

            return Ok(dt);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Write()
        {
            var stream = new StreamReader(_path);

            var list = csvService.ReadAs<CsvObject>(stream);

            var write = await csvService.WriteAsync(list);

            return File(write, "application/octet-stream", "DataExport.csv"); // returns a FileStreamResult
        }

        [HttpGet("export_dt")]
        public async Task<IActionResult> ExportDatatable()
        {
            DataTable dt = new DataTable();
            dt.Clear();
            dt.Columns.Add("Id");
            dt.Columns.Add("Name");

            DataRow row = dt.NewRow();
            row["Id"] = "1";
            row["Name"] = "test";
            dt.Rows.Add(row);

            DataRow row2 = dt.NewRow();
            row2["Id"] = "2";
            row2["Name"] = "test 2";
            dt.Rows.Add(row2);

            DataRow row3 = dt.NewRow();
            row3["Id"] = "3";
            dt.Rows.Add(row3);

            DataRow row4 = dt.NewRow();
            row4["Name"] = "test 4";
            dt.Rows.Add(row4);

            Stream stream = await csvService.WriteAsync(dt);
            return File(stream, "application/octet-stream", "DataTableCsvExport.csv"); // returns a FileStreamResult
        }
    }

    public class CsvObject
    {
        [Index(0)]
        public long BroadLogRcpId { get; set; }

        [Index(1)]
        public long Visible_Card { get; set; }

        [Index(2)]
        public string C_MOBILE { get; set; } = null!;

        [Index(3)]
        public string C_NAME { get; set; } = null!;

        public object? Test { get; set; }
    }
}


