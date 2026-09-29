using ClosedXML.Excel;
using Light.File.Excel;
using Light.Infrastructure.Excel;
using System.Data;
using System.Globalization;

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
    public void ReadAsObjects_FractionalCell_ReturnsDouble()
    {
        // Previously Convert.ToInt64(cell.Value.ToString()) threw FormatException for fractional numbers.
        var excel = new ExcelService();
        var stream = BuildWorkbook("Data", ["Value"], [3.7]);

        var rows = excel.ReadAsObjects(stream, "Data");

        Assert.That(rows[0]["Value"], Is.TypeOf<double>());
        Assert.That(rows[0]["Value"], Is.EqualTo(3.7));
    }

    [Test]
    public void ReadAsObjects_DateAndBoolCells_ReturnTypedValues_CultureIndependent()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var excel = new ExcelService();
            var date = new DateTime(2026, 4, 3, 10, 30, 0);
            var stream = BuildWorkbook("Data", ["When", "Flag"], [date, true]);

            var rows = excel.ReadAsObjects(stream, "Data");

            Assert.That(rows[0]["When"], Is.EqualTo(date));
            Assert.That(rows[0]["Flag"], Is.EqualTo(true));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    private class TypedExcelRow
    {
        public decimal Price { get; set; }

        public double Ratio { get; set; }

        public int Count { get; set; }

        public int? Optional { get; set; }

        public DateTime When { get; set; }

        public DateTime? MaybeWhen { get; set; }

        public bool Active { get; set; }

        public string? Name { get; set; }
    }

    [Test]
    public void ReadAs_TypedCells_ConvertToTargetTypes()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var excel = new ExcelService();
            var date = new DateTime(2026, 4, 3);
            var stream = BuildWorkbook("Data",
                ["Price", "Ratio", "Count", "Optional", "When", "MaybeWhen", "Active", "Name"],
                [12.5, 0.25, 7, 3, date, date, true, "A"]);

            var row = excel.ReadAs<TypedExcelRow>(stream, "Data").Single();

            Assert.Multiple(() =>
            {
                Assert.That(row.Price, Is.EqualTo(12.5m));
                Assert.That(row.Ratio, Is.EqualTo(0.25));
                Assert.That(row.Count, Is.EqualTo(7));
                Assert.That(row.Optional, Is.EqualTo(3));
                Assert.That(row.When, Is.EqualTo(date));
                Assert.That(row.MaybeWhen, Is.EqualTo(date));
                Assert.That(row.Active, Is.True);
                Assert.That(row.Name, Is.EqualTo("A"));
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Test]
    public void ReadAs_TextCells_ParseWithInvariantCulture()
    {
        var excel = new ExcelService();
        var stream = BuildWorkbook("Data", ["Price", "When"], ["12.5", "2026-04-03"]);

        var row = excel.ReadAs<TypedExcelRow>(stream, "Data").Single();

        Assert.That(row.Price, Is.EqualTo(12.5m));
        Assert.That(row.When, Is.EqualTo(new DateTime(2026, 4, 3)));
    }

    [TestCase("Count")]
    [TestCase("Optional")]
    public void ReadAs_FractionalNumberIntoIntegralProperty_Throws(string header)
    {
        var excel = new ExcelService();
        var stream = BuildWorkbook("Data", [header], [3.7]);

        var ex = Assert.Throws<FormatException>(() => excel.ReadAs<TypedExcelRow>(stream, "Data").ToList());

        Assert.That(ex!.Message, Does.Contain("A2").And.Contain("3.7"));
    }

    [Test]
    public void ReadAs_WholeNumberIntoIntegralProperty_AndFractionIntoDouble_Succeed()
    {
        var excel = new ExcelService();
        var stream = BuildWorkbook("Data", ["Count", "Ratio"], [7.0, 3.7]);

        var row = excel.ReadAs<TypedExcelRow>(stream, "Data").Single();

        Assert.That(row.Count, Is.EqualTo(7));
        Assert.That(row.Ratio, Is.EqualTo(3.7));
    }

    [Test]
    public void ReadAs_BlankCells_LeaveNonNullableDefault_AndNullableNull()
    {
        var excel = new ExcelService();
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Data");
        ws.Cell(1, 1).Value = "Name";
        ws.Cell(1, 2).Value = "Count";
        ws.Cell(1, 3).Value = "Optional";
        ws.Cell(2, 1).Value = "A"; // Count/Optional left blank
        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;

        var row = excel.ReadAs<TypedExcelRow>(stream, "Data").Single();

        Assert.That(row.Count, Is.EqualTo(0));
        Assert.That(row.Optional, Is.Null);
    }

    [Test]
    public void BlankHeaderCell_DoesNotShiftColumnMapping()
    {
        var excel = new ExcelService();
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Data");
        ws.Cell(1, 1).Value = "Id";
        // B1 header left blank
        ws.Cell(1, 3).Value = "Name";
        ws.Cell(2, 1).Value = 1;
        ws.Cell(2, 2).Value = "ignored";
        ws.Cell(2, 3).Value = "A";
        var bytes = new MemoryStream();
        wb.SaveAs(bytes);

        var typed = excel.ReadAs<ExcelRow>(new MemoryStream(bytes.ToArray()), "Data").Single();
        var loose = excel.ReadAsObjects(new MemoryStream(bytes.ToArray()), "Data").Single();
        var table = excel.ReadAsDataTable(new MemoryStream(bytes.ToArray()), "Data");

        Assert.Multiple(() =>
        {
            Assert.That(typed.Id, Is.EqualTo(1));
            Assert.That(typed.Name, Is.EqualTo("A"));
            Assert.That(loose["Name"], Is.EqualTo("A"));
            Assert.That(table.Rows[0]["Name"], Is.EqualTo("A"));
        });
    }

    [Test]
    public void ReadAs_DuplicateHeaders_FirstColumnWins()
    {
        var excel = new ExcelService();
        var stream = BuildWorkbook("Data", ["Id", "Name", "Name"], [1, "first", "second"]);

        var row = excel.ReadAs<ExcelRow>(stream, "Data").Single();

        Assert.That(row.Name, Is.EqualTo("first"));
    }

    private class ReadOnlyPropRow
    {
        public long Id { get; set; }

        public string Computed => $"#{Id}";
    }

    [Test]
    public void ReadAs_ReadOnlyProperty_IsSkipped()
    {
        var excel = new ExcelService();
        var stream = BuildWorkbook("Data", ["Id", "Computed"], [5, "x"]);

        var row = excel.ReadAs<ReadOnlyPropRow>(stream, "Data").Single();

        Assert.That(row.Id, Is.EqualTo(5));
        Assert.That(row.Computed, Is.EqualTo("#5"));
    }

    [Test]
    public void Export_String_IsNotTreatedAsCharSequence()
    {
        var excel = new ExcelService();

        // previously a string was treated as IEnumerable<char>; it must now be wrapped as a single value
        Assert.DoesNotThrow(() => excel.Export(("abc", "Data")));
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
