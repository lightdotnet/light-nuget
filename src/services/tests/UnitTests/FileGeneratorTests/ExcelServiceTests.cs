using ClosedXML.Excel;
using Light.File.Excel;
using Light.Infrastructure.Excel;
using System.Data;

namespace UnitTests.FileGeneratorTests;

public class ExcelServiceTests
{
    private class ExcelRow
    {
        public long Id { get; set; }

        public string Name { get; set; } = null!;
    }

    private static MemoryStream BuildWorkbook(string sheetName, string[] headers, params object[][] rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);

        for (int c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        for (int r = 0; r < rows.Length; r++)
        {
            for (int c = 0; c < rows[r].Length; c++)
            {
                var cell = ws.Cell(r + 2, c + 1);
                cell.Value = rows[r][c] switch
                {
                    int i => (XLCellValue)i,
                    long l => (XLCellValue)l,
                    double d => (XLCellValue)d,
                    bool b => (XLCellValue)b,
                    DateTime dt => (XLCellValue)dt,
                    string s => (XLCellValue)s,
                    _ => (XLCellValue)rows[r][c].ToString(),
                };
            }
        }

        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    [Test]
    public void Export_SingleEnumerable_DefaultsSheetName_Sheet1()
    {
        var excel = new ExcelService();
        var list = new List<ExcelRow> { new() { Id = 1, Name = "A" } };

        using var wb = new XLWorkbook(excel.Export((list, null)));

        Assert.That(wb.Worksheets.Contains("sheet1"), Is.True);
    }

    [Test]
    public void Export_SinglePlainObject_WrapsAsOneRowSheet()
    {
        var excel = new ExcelService();
        var single = new ExcelRow { Id = 1, Name = "A" };

        using var wb = new XLWorkbook(excel.Export((single, "Sheet1")));
        var ws = wb.Worksheet("Sheet1");

        // Header row + exactly one data row.
        Assert.That(ws.RowsUsed().Count(), Is.EqualTo(2));
    }

    [Test]
    public void Export_DataTable_WritesViaInsertTable()
    {
        var excel = new ExcelService();
        var table = new DataTable();
        table.Columns.Add("Id");
        table.Columns.Add("Name");
        table.Rows.Add("1", "A");

        using var wb = new XLWorkbook(excel.Export((table, "Data")));
        var ws = wb.Worksheet("Data");

        Assert.That(ws.Cell(2, 1).GetString(), Is.EqualTo("1"));
        Assert.That(ws.Cell(2, 2).GetString(), Is.EqualTo("A"));
    }

    [Test]
    public void Export_MultipleSheets_UnnamedFallsBackToSheetIndex()
    {
        var excel = new ExcelService();
        var list1 = new List<ExcelRow> { new() { Id = 1, Name = "A" } };
        var list2 = new List<ExcelRow> { new() { Id = 2, Name = "B" } };

        using var wb = new XLWorkbook(excel.Export((list1, null), (list2, null)));

        Assert.That(wb.Worksheets.Contains("sheet1"), Is.True);
        Assert.That(wb.Worksheets.Contains("sheet2"), Is.True);
    }

    [Test]
    public void ReadAsDataTable_RoundTrip_AllValuesAreStrings()
    {
        var excel = new ExcelService();
        var stream = BuildWorkbook("Data", ["Id", "Name"], [1, "A"]);

        var dt = excel.ReadAsDataTable(stream, "Data");

        Assert.That(dt.Rows[0]["Id"], Is.TypeOf<string>());
        Assert.That(dt.Rows[0]["Id"], Is.EqualTo("1"));
        Assert.That(dt.Rows[0]["Name"], Is.EqualTo("A"));
    }

    [Test]
    public void ReadAsDataTable_SheetNotFound_Throws()
    {
        var excel = new ExcelService();
        var stream = BuildWorkbook("Data", ["Id"], [1]);

        Assert.Catch(() => excel.ReadAsDataTable(stream, "Missing"));
    }

    [Test]
    public void ReadAs_SheetNotFound_ReturnsEmpty()
    {
        // Documented inconsistency: unlike ReadAsDataTable/ReadAsObjects, ReadAs<T> checks
        // Worksheets.Contains(sheetName) up front and returns empty instead of throwing.
        var excel = new ExcelService();
        var stream = BuildWorkbook("Data", ["Id"], [1]);

        var result = excel.ReadAs<ExcelRow>(stream, "Missing");

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ReadAsObjects_SheetNotFound_Throws()
    {
        var excel = new ExcelService();
        var stream = BuildWorkbook("Data", ["Id"], [1]);

        Assert.Catch(() => excel.ReadAsObjects(stream, "Missing"));
    }

    [Test]
    public void ReadAs_RoundTrip_MapsByPropertyName()
    {
        var excel = new ExcelService();
        var stream = BuildWorkbook("Data", ["Id", "Name"], [1, "A"]);

        var rows = excel.ReadAs<ExcelRow>(stream, "Data").ToList();

        Assert.That(rows[0].Id, Is.EqualTo(1));
        Assert.That(rows[0].Name, Is.EqualTo("A"));
    }

    [Test]
    public void ReadAs_WithColumnOptions_MapsRenamedHeader()
    {
        var excel = new ExcelService();
        var stream = BuildWorkbook("Data", ["Identifier", "Name"], [1, "A"]);
        var options = new ColumnOptions<ExcelRow>().SetColumn(r => r.Id, "Identifier");

        var rows = excel.ReadAs(stream, "Data", options).ToList();

        Assert.That(rows[0].Id, Is.EqualTo(1));
    }

    [Test]
    public void ReadAsObjects_IntegerCell_ReturnsInt64()
    {
        var excel = new ExcelService();
        var stream = BuildWorkbook("Data", ["Value"], [3]);

        var rows = excel.ReadAsObjects(stream, "Data");

        Assert.That(rows[0]["Value"], Is.EqualTo(3L));
    }

    [Test]
    public void ReadAsObjects_FractionalCell_ThrowsFormatException()
    {
        // Regression: any IsNumber cell is converted with Convert.ToInt64(cell.Value.ToString()),
        // which requires an exact integer string — a fractional value like "3.7" throws
        // FormatException rather than being rounded or truncated.
        var excel = new ExcelService();
        var stream = BuildWorkbook("Data", ["Value"], [3.7]);

        Assert.Throws<FormatException>(() => excel.ReadAsObjects(stream, "Data"));
    }

    [Test]
    public void ReadAsObjects_TextCell_ReturnsString()
    {
        var excel = new ExcelService();
        var stream = BuildWorkbook("Data", ["Value"], ["hello"]);

        var rows = excel.ReadAsObjects(stream, "Data");

        Assert.That(rows[0]["Value"], Is.EqualTo("hello"));
    }
}
