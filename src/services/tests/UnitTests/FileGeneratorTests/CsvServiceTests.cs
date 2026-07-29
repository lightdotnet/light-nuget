using Light.FileGenerator.Infrastructure.Csv;
using NUnit.Framework;
using System.Data;
using System.Globalization;
using System.Text;

namespace UnitTests.FileGeneratorTests;

public class CsvServiceTests
{
    private class ObjectValueRow
    {
        public object? Value { get; set; }
    }

    private class TypedRow
    {
        public long Id { get; set; }

        public string Name { get; set; } = null!;
    }

    private static StreamReader ReaderFor(string csv) =>
        new(new MemoryStream(Encoding.UTF8.GetBytes(csv)));

    [Test]
    public void ReadHeaders_ReturnsHeaderArray()
    {
        var csv = new CsvService();

        var headers = csv.ReadHeaders(ReaderFor("Id,Name\n1,A\n"));

        Assert.That(headers, Is.EqualTo(new[] { "Id", "Name" }));
    }

    [Test]
    public void ReadAs_ObjectProperty_IntegerText_ReturnsLong()
    {
        var csv = new CsvService();

        var rows = csv.ReadAs<ObjectValueRow>(ReaderFor("Value\n42\n")).ToList();

        Assert.That(rows[0].Value, Is.TypeOf<long>());
        Assert.That(rows[0].Value, Is.EqualTo(42L));
    }

    [Test]
    public void ReadAs_ObjectProperty_FractionalText_ReturnsDouble_NotDecimal()
    {
        // Regression: ObjectConverter checks long -> double -> decimal -> bool -> DateTime, but
        // double.TryParse succeeds for ordinary decimals first, so the decimal branch is unreachable.
        var csv = new CsvService();

        var rows = csv.ReadAs<ObjectValueRow>(ReaderFor("Value\n3.14\n")).ToList();

        Assert.That(rows[0].Value, Is.TypeOf<double>());
        Assert.That(rows[0].Value, Is.EqualTo(3.14));
    }

    [Test]
    public void ReadAs_ObjectProperty_UnparsableText_ReturnsString()
    {
        var csv = new CsvService();

        var rows = csv.ReadAs<ObjectValueRow>(ReaderFor("Value\nhello\n")).ToList();

        Assert.That(rows[0].Value, Is.EqualTo("hello"));
    }

    [Test]
    public void ReadAs_ObjectProperty_DateText_UsesInvariantCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            // German culture reads "03/04/2026" ambiguously; invariant culture must still win.
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            var csv = new CsvService();
            var rows = csv.ReadAs<ObjectValueRow>(ReaderFor("Value\n2026-04-03\n")).ToList();

            Assert.That(rows[0].Value, Is.TypeOf<DateTime>());
            Assert.That(rows[0].Value, Is.EqualTo(new DateTime(2026, 4, 3)));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Test]
    public void Read_Generic_ReturnsHeadersAndRows()
    {
        var csv = new CsvService();

        var data = csv.Read<TypedRow>(ReaderFor("Id,Name\n1,A\n2,B\n"));

        Assert.That(data, Is.Not.Null);
        Assert.That(data!.Headers, Is.EqualTo(new[] { "Id", "Name" }));
        Assert.That(data.Rows.Select(r => r.Name), Is.EqualTo(new[] { "A", "B" }));
    }

    [Test]
    public void Read_NonGeneric_ValuesAlwaysStrings_NotConverted()
    {
        // Regression: the untyped Read(...) overload registers ObjectConverter but reads values via
        // csv.GetField(header), which always returns raw strings — the converter is never applied.
        var csv = new CsvService();

        var data = csv.Read(ReaderFor("Value\n3.14\n"));

        Assert.That(data, Is.Not.Null);
        var firstRow = data!.Rows.First();
        Assert.That(firstRow["Value"], Is.TypeOf<string>());
        Assert.That(firstRow["Value"], Is.EqualTo("3.14"));
    }

    [Test]
    public async Task WriteAsync_Generic_RoundTrip_ReadAsMatchesOriginal()
    {
        var csv = new CsvService();
        var original = new[] { new TypedRow { Id = 1, Name = "A" }, new TypedRow { Id = 2, Name = "B" } };

        var stream = await csv.WriteAsync(original);

        var roundTripped = csv.ReadAs<TypedRow>(new StreamReader(stream)).ToList();
        Assert.That(roundTripped.Select(r => (r.Id, r.Name)), Is.EqualTo(original.Select(r => (r.Id, r.Name))));
    }

    [Test]
    public async Task WriteAsync_DataTable_RoundTrip_ReadDictionaryMatchesItemArray()
    {
        var csv = new CsvService();
        var table = new DataTable();
        table.Columns.Add("Id");
        table.Columns.Add("Name");
        table.Rows.Add("1", "A");

        var stream = await csv.WriteAsync(table);

        var data = csv.Read(new StreamReader(stream));
        Assert.That(data, Is.Not.Null);
        var firstRow = data!.Rows.First();
        Assert.That(firstRow["Id"], Is.EqualTo("1"));
        Assert.That(firstRow["Name"], Is.EqualTo("A"));
    }

    [Test]
    public async Task WriteAsync_ExcludeHeader_OmitsHeaderRow()
    {
        var csv = new CsvService();
        var original = new[] { new TypedRow { Id = 1, Name = "A" } };

        var stream = await csv.WriteAsync(original, excludeHeader: true);

        using var reader = new StreamReader(stream);
        var firstLine = await reader.ReadLineAsync();
        Assert.That(firstLine, Is.EqualTo("1,A"));
    }
}
