using Light.Extensions.DependencyInjection;
using Light.File.Csv;
using Light.Infrastructure.Csv;
using System.Data;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
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
    public void ReadAs_ObjectProperty_FractionalText_ReturnsDecimal()
    {
        // ObjectConverter checks long -> decimal -> double, all with InvariantCulture.
        var csv = new CsvService();

        var rows = csv.ReadAs<ObjectValueRow>(ReaderFor("Value\n3.14\n")).ToList();

        Assert.That(rows[0].Value, Is.TypeOf<decimal>());
        Assert.That(rows[0].Value, Is.EqualTo(3.14m));
    }

    [Test]
    public void ReadAs_ObjectProperty_ExponentText_ReturnsDouble()
    {
        var csv = new CsvService();

        var rows = csv.ReadAs<ObjectValueRow>(ReaderFor("Value\n1.5E+3\n")).ToList();

        Assert.That(rows[0].Value, Is.TypeOf<double>());
        Assert.That(rows[0].Value, Is.EqualTo(1500d));
    }

    [Test]
    public void ReadAs_ObjectProperty_FractionalText_IgnoresCurrentCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            var rows = new CsvService().ReadAs<ObjectValueRow>(ReaderFor("Value\n3.14\n")).ToList();

            Assert.That(rows[0].Value, Is.EqualTo(3.14m));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
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

    private class TextRow
    {
        public string Name { get; set; } = null!;

        public long Amount { get; set; }
    }

    [Test]
    public async Task WriteAsync_Generic_EscapesFormulaInjection_ButNotNegativeNumbers()
    {
        var csv = new CsvService();
        var rows = new[]
        {
            new TextRow { Name = "=HYPERLINK(\"http://evil\")", Amount = -5 },
            new TextRow { Name = "@SUM(A1)", Amount = 1 },
            new TextRow { Name = "normal", Amount = 2 },
        };

        using var reader = new StreamReader(await csv.WriteAsync(rows, excludeHeader: true));
        var text = await reader.ReadToEndAsync();
        var lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Multiple(() =>
        {
            Assert.That(lines[0], Does.StartWith("\"'=HYPERLINK").Or.StartWith("'=HYPERLINK"));
            Assert.That(lines[0], Does.EndWith(",-5"));
            Assert.That(lines[1], Does.StartWith("'@SUM(A1)").Or.StartWith("\"'@SUM(A1)"));
            Assert.That(lines[2], Is.EqualTo("normal,2"));
        });
    }

    [Test]
    public async Task WriteAsync_DataTable_EscapesFormulaInjection()
    {
        var csv = new CsvService();
        var table = new DataTable();
        table.Columns.Add("Name");
        table.Rows.Add("+cmd|' /C calc'!A0");

        using var reader = new StreamReader(await csv.WriteAsync(table, excludeHeader: true));
        var firstLine = await reader.ReadLineAsync();

        Assert.That(firstLine, Does.Contain("'+cmd"));
    }

    [Test]
    public async Task WriteAsync_InjectionOptionsNone_WritesRawValue()
    {
        var csv = new CsvService { InjectionOptions = CsvHelper.Configuration.InjectionOptions.None };
        var rows = new[] { new TextRow { Name = "=1+1", Amount = 1 } };

        using var reader = new StreamReader(await csv.WriteAsync(rows, excludeHeader: true));
        var firstLine = await reader.ReadLineAsync();

        Assert.That(firstLine, Is.EqualTo("=1+1,1"));
    }

    private static readonly string[] InjectionValues =
        ["-abc", "=1+1", "@SUM(A1)", "+cmd|' /C calc'!A0", "\tTab", "=a,b", "'-already", "''=twice", "'plain", "normal", "-5"];

    [Test]
    public async Task WriteAsync_Generic_EscapedValues_RoundTripThroughReadAs()
    {
        var csv = new CsvService();
        var rows = InjectionValues.Select((v, i) => new TextRow { Name = v, Amount = -i }).ToList();

        var read = csv.ReadAs<TextRow>(await csv.WriteAsync(rows)).ToList();

        Assert.That(read.Select(r => r.Name), Is.EqualTo(InjectionValues));
        Assert.That(read.Select(r => r.Amount), Is.EqualTo(rows.Select(r => r.Amount)));
    }

    [Test]
    public async Task WriteAsync_DataTable_EscapedValuesAndHeaders_RoundTripThroughRead()
    {
        var csv = new CsvService();
        var table = new DataTable();
        table.Columns.Add("=Header");
        foreach (var value in InjectionValues) table.Rows.Add(value);

        var data = csv.Read(await csv.WriteAsync(table))!;

        Assert.That(data.Headers, Is.EqualTo(new[] { "=Header" }));
        Assert.That(data.Rows.Select(r => r["=Header"]), Is.EqualTo(InjectionValues));
    }

    [Test]
    public void Read_EscapeMode_StripsOnlySingleQuoteBeforeInjectionCharacter()
    {
        var csv = new CsvService();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name\n'-abc\n'abc\n''=x\n"));

        var names = csv.ReadAs<TextRow>(stream).Select(r => r.Name).ToList();

        Assert.That(names, Is.EqualTo(new[] { "-abc", "'abc", "'=x" }));
    }

    [Test]
    public void Read_InjectionOptionsNone_ReadsFieldsVerbatim()
    {
        var csv = new CsvService { InjectionOptions = CsvHelper.Configuration.InjectionOptions.None };
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name\n'-abc\n"));

        Assert.That(csv.ReadAs<TextRow>(stream).Single().Name, Is.EqualTo("'-abc"));
    }

    [Test]
    public async Task AddFileGenerator_Options_CanOptOutOfInjectionEscaping()
    {
        var services = new ServiceCollection();
        services.AddFileGenerator(o => o.CsvInjectionOptions = CsvHelper.Configuration.InjectionOptions.None);
        using var provider = services.BuildServiceProvider();

        var csv = (CsvService)provider.GetRequiredService<ICsvService>();

        using var reader = new StreamReader(await csv.WriteAsync(new[] { new TextRow { Name = "=1+1", Amount = 1 } }, excludeHeader: true));

        Assert.That(csv.InjectionOptions, Is.EqualTo(CsvHelper.Configuration.InjectionOptions.None));
        Assert.That(await reader.ReadLineAsync(), Is.EqualTo("=1+1,1"));
    }

    [Test]
    public void AddFileGenerator_Defaults_KeepEscaping()
    {
        var services = new ServiceCollection();
        services.AddFileGenerator();
        services.AddFileGenerator(_ => { });
        using var provider = services.BuildServiceProvider();

        var all = provider.GetServices<ICsvService>().Cast<CsvService>().ToList();

        Assert.That(all.Select(c => c.InjectionOptions),
            Is.All.EqualTo(CsvHelper.Configuration.InjectionOptions.Escape));
    }

    [Test]
    public void StreamOverloads_LeaveCallerStreamOpen()
    {
        var csv = new CsvService();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Id,Name\n1,A\n"));

        var headers = csv.ReadHeaders(stream);
        Assert.That(stream.CanRead, Is.True);

        stream.Position = 0;
        var typed = csv.ReadAs<TypedRow>(stream).ToList();
        Assert.That(stream.CanRead, Is.True);

        stream.Position = 0;
        var generic = csv.Read<TypedRow>(stream);
        Assert.That(stream.CanRead, Is.True);

        stream.Position = 0;
        var dictionary = csv.Read(stream);

        Assert.Multiple(() =>
        {
            Assert.That(stream.CanRead, Is.True);
            Assert.That(headers, Is.EqualTo(new[] { "Id", "Name" }));
            Assert.That(typed.Single().Name, Is.EqualTo("A"));
            Assert.That(generic!.Rows.Single().Name, Is.EqualTo("A"));
            Assert.That(dictionary!.Rows.Single()["Name"], Is.EqualTo("A"));
        });
    }
}
