[← Back to main README](https://github.com/lightdotnet/light-nuget#readme)

# Lightsoft.FileGenerator

CSV and Excel read/write helpers behind two small service interfaces — `ICsvService` (built on CsvHelper) and `IExcelService` (built on ClosedXML) — plus a DI extension to register both.

- **NuGet package id / assembly name:** `Lightsoft.FileGenerator` (no explicit `PackageId`, so it defaults to `AssemblyName`)
- **Root namespace:** `Light` — types live under `Light.File.Csv`, `Light.File.Excel`, `Light.Infrastructure.Csv`, `Light.Infrastructure.Excel`, and `Light.Extensions.DependencyInjection`
- **Target framework:** netstandard2.1
- **Dependencies:** `ClosedXML` (`0.*`), `CsvHelper` (`33.*`), `Microsoft.Extensions.DependencyInjection.Abstractions`. No `ProjectReference`s — this is a leaf project.

## What's in this package

| Type | Namespace | Purpose |
|---|---|---|
| `ICsvService` | `Light.File.Csv` | Contract for reading CSV headers/records (typed or dictionary-based) and writing an `IEnumerable<T>` or `DataTable` back out as a CSV stream. |
| `CsvData<T>` | `Light.File.Csv` | DTO returned by `Read<T>`: `Headers` (`string[]`) + `Rows` (`IEnumerable<T>`). |
| `DictionaryData` | `Light.File.Csv` | `CsvData<IDictionary<string, object?>>` — the untyped row shape returned by the non-generic `Read` overloads. |
| `IExcelService` | `Light.File.Excel` | Contract for exporting one or more sheets (objects or `DataTable`s) to an `.xlsx` stream, and reading an `.xlsx` stream back as a `DataTable`, typed objects, or loose dictionaries. |
| `Worksheet` | `Light.File.Excel` | Simple DTO pairing `Data` (`object`) with an optional `SheetName`, used by the `ExcelExtensions.Export(Worksheet[])` overload. |
| `ColumnOptions<T>` | `Light.File.Excel` | Fluent builder mapping a `T` property (via a member-access expression) to an explicit Excel column header, for use with `IExcelService.ReadAs<T>`. |
| `ExcelExtensions` | `Light.File.Excel` | Static sugar over `IExcelService.Export`: a single-list `Export<T>(list, sheetName)` overload and an `Export(params Worksheet[])` overload. |
| `CsvService` | `Light.Infrastructure.Csv` | `ICsvService` implementation built on CsvHelper, pinned to `CultureInfo.InvariantCulture`. |
| `ObjectConverter` | `Light.Infrastructure.Csv` | CsvHelper `DefaultTypeConverter` registered for `object`-typed properties; sniffs a raw field string (invariant culture) into `long` → `decimal` → `double` → `bool` → `DateTime`, else leaves it as `string`; empty → `null`. |
| `ExcelService` | `Light.Infrastructure.Excel` | `IExcelService` implementation built on ClosedXML (`XLWorkbook`). |
| `Extensions` (internal) | `Light.Infrastructure.Excel` | Internal helpers used by `ExcelService`: workbook → stream, worksheet resolution, header extraction, and value conversion for `ReadAs<T>`. |
| `ServiceCollectionExtensions` | `Light.Extensions.DependencyInjection` | `AddFileGenerator()` / `AddFileGenerator(Action<FileGeneratorOptions>)` — registers both services. |
| `FileGeneratorOptions` | `Light.Infrastructure` | Options for `AddFileGenerator(Action<FileGeneratorOptions>)`: `CsvInjectionOptions` (default `Escape`). |

## `ICsvService` / `CsvService`

`CsvService` configures CsvHelper with `HasHeaderRecord = true`, `HeaderValidated = null` (missing headers don't throw) and `MissingFieldFound = null` (missing fields in a row don't throw) — reading is intentionally lenient. All reading/writing uses `CultureInfo.InvariantCulture`.

- `ReadHeaders(StreamReader | Stream)` — reads only the header row, returns `string[]?`.
- `ReadAs<T>(StreamReader | Stream)` — maps every row directly to `T` via `CsvReader.GetRecords<T>()`. Registers `ObjectConverter` so any `object`-typed property on `T` gets type-sniffed from its raw text.
- `Read<T>(StreamReader | Stream)` — same mapping as `ReadAs<T>`, but also returns the header row, wrapped in `CsvData<T>`; returns `null` if no header record was found.
- `Read(StreamReader | Stream)` — non-generic; returns `DictionaryData` (headers + one `Dictionary<string, object?>` per row, keyed by header name), or `null` if no headers.
- `WriteAsync<T>(IEnumerable<T> records, bool excludeHeader = false)` — writes a header row (unless `excludeHeader`) then each record, returning a rewound `MemoryStream`.
- `WriteAsync(DataTable table, bool excludeHeader = false)` — same, but source is a `DataTable`: writes each `DataColumn.ColumnName` as the header, then each `DataRow.ItemArray` value via `csv.WriteField`.
- `InjectionOptions` (`CsvHelper.Configuration.InjectionOptions`, default `Escape`) — CSV/formula-injection protection. On write (both `WriteAsync` overloads), text fields starting with `=`, `@`, `+`, `-`, tab or CR are prefixed with `'` so Excel/LibreOffice won't evaluate them as formulas; plain numeric fields (e.g. `-5`, `+1.5`) are left untouched (a number with a leading tab/CR such as `"\t5"` is still escaped), and a value that already starts with `'` + one of those characters gets one more `'`. On read (all `Read*` methods, headers included), one leading `'` is removed from fields that start with `'` (one or more) followed by one of those characters, so a file written by `CsvService` round-trips exactly (`"-abc"` is read back as `"-abc"`, not `"'-abc"`). **Behavior change:** a field such as `'=x` in a CSV from another source is now read as `=x`. Set `InjectionOptions.None` (via `new CsvService { InjectionOptions = InjectionOptions.None }` or `AddFileGenerator(o => o.CsvInjectionOptions = InjectionOptions.None)`) to get the old raw output and verbatim reads.

The `Stream` overloads leave the caller's stream **open** (the internal `StreamReader` is created with `leaveOpen: true`); the caller owns and disposes the stream. The `StreamReader` overloads still dispose the reader you pass in (via the `CsvReader`).

```csharp
public class ImportRow
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public object? Extra { get; set; } // goes through ObjectConverter
}

public class CsvController(ICsvService csvService) : ControllerBase
{
    [HttpGet("import")]
    public IActionResult Import(string path)
    {
        using var reader = new StreamReader(path);
        CsvData<ImportRow>? data = csvService.Read<ImportRow>(reader);
        return Ok(data);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var rows = new[]
        {
            new ImportRow { Id = 1, Name = "First" },
            new ImportRow { Id = 2, Name = "Second" },
        };

        Stream csv = await csvService.WriteAsync(rows);
        return File(csv, "application/octet-stream", "rows.csv");
    }
}
```

## `IExcelService` / `ExcelService`

- `Export(params (object Data, string? SheetName)[] sheets)` — one sheet per tuple. A `DataTable` is written with `InsertTable(dt, true)`; any other `IEnumerable` is written the same way via ClosedXML's object-table insertion; a single non-enumerable object is wrapped as a one-row sheet (`new object[] { data }`) before insertion. Sheets without an explicit name get `"sheet{n}"`. All used columns are auto-fit to their contents before the workbook is returned as a stream.
- `ReadAsDataTable(Stream streamData, string? sheetName = null)` — reads the first row as column headers and every subsequent used row into a `DataTable`; every cell value is captured via `.ToString()`, so all `DataTable` values come back as `string`.
- `ReadAs<T>(Stream streamData, string? sheetName = null, ColumnOptions<T>? options = null)` — maps each row to a new `T` (via `Activator.CreateInstance`, so `T` needs a public parameterless constructor), matching columns to public settable properties by name unless overridden with `ColumnOptions<T>.SetColumn` (read-only properties are skipped; if a header text is duplicated the first column wins, same as `ReadAsObjects`). Values are converted from the cell's typed value (number/date/boolean/text) to the property type — including nullable types, `decimal`, `double`, `int`/`long`, enums, `DateTime`/`DateTimeOffset`, `Guid` — using `CultureInfo.InvariantCulture` for text cells. Blank cells leave non-nullable value-type properties at their default and set nullable ones to `null`; a fractional number mapped to an integral (or enum) property throws `FormatException` naming the cell (e.g. `3.7` into `int`), instead of silently rounding. A value outside the target range (e.g. `300` into `byte`, `3e10` into `int`) throws `OverflowException`, and any other value that can't be converted (bad text, a time cell into `int`, ...) throws `FormatException`; both name the cell and wrap the original exception (previously raw `OverflowException`/`InvalidCastException`/`ArgumentException` without the cell address). Date and numeric (OADate) cells mapped to `DateTimeOffset` keep their wall-clock value with the machine's local offset (`new DateTimeOffset(dateTime)`). Time cells map to `TimeSpan`, or to `double`/`decimal`/`float` as Excel's fraction of a day; `object` properties get the text. If `sheetName` is supplied but not present in the workbook, this returns an **empty** sequence rather than throwing.
- `ReadAsObjects(Stream streamData, string? sheetName = null)` — maps each row to a loose `Dictionary<string, object>` using the cell's typed value: integral numbers → `long`, fractional numbers → `double`, date cells → `DateTime`, boolean cells → `bool`, everything else via `.ToString()`. If a header text is duplicated, the first column wins (later columns with the same header are ignored), consistent with `ReadAs<T>`; previously the last one silently overwrote it.

`ExcelExtensions` adds two conveniences over the tuple-based `Export`:

```csharp
public class ExcelController(IExcelService excelService) : ControllerBase
{
    [HttpGet("export")]
    public IActionResult Export()
    {
        var products = new[]
        {
            new { Id = 1, Name = "Widget" },
            new { Id = 2, Name = "Gadget" },
        };

        Stream xlsx = excelService.Export(products, sheetName: "Products"); // ExcelExtensions.Export<T>
        return File(xlsx, "application/octet-stream", "products.xlsx");
    }

    [HttpGet("import")]
    public IActionResult Import(IFormFile file)
    {
        var options = new ColumnOptions<ProductRow>()
            .SetColumn(p => p.Id, "Product Id");

        using var stream = file.OpenReadStream();
        IEnumerable<ProductRow> rows = excelService.ReadAs(stream, sheetName: "Products", options: options);
        return Ok(rows);
    }
}

public class ProductRow
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}
```

## Dependency injection

```csharp
services.AddFileGenerator();
// or, to configure the CSV service:
services.AddFileGenerator(o => o.CsvInjectionOptions = InjectionOptions.None);
```

`AddFileGenerator()` (in `Light.Extensions.DependencyInjection`) registers `ICsvService -> CsvService` and `IExcelService -> ExcelService`, both `AddTransient`. The `AddFileGenerator(Action<FileGeneratorOptions>)` overload does the same, but builds the `CsvService` from `FileGeneratorOptions` (`Light.Infrastructure`): `CsvInjectionOptions` (default `Escape`) sets `CsvService.InjectionOptions`, e.g. `services.AddFileGenerator(o => o.CsvInjectionOptions = InjectionOptions.None);` to opt out of formula-injection escaping.

## Notes

- `ObjectConverter.ConvertFromString` checks types in this order, all with `CultureInfo.InvariantCulture`: `long` → `decimal` → `double` (exponent / out-of-`decimal`-range values) → `bool` → `DateTime`. **Behavior change:** ordinary fractional text such as `"3.14"` now comes back as `decimal` (previously `double`, parsed with the current culture).
- `ICsvService.Read(StreamReader | Stream)` (the non-generic, `DictionaryData`-returning overload) registers `ObjectConverter` on the reader but never actually uses it: rows are populated via `csv.GetField(header)`, which returns raw text. So `DictionaryData.Rows` values are always `string`, unlike `ReadAs<T>`/`Read<T>`, where `object`-typed target properties are converted.
- `IExcelService.ReadAsObjects` reads numeric cells via ClosedXML's typed accessor: integral values come back as `long` (as before), fractional values as `double` (previously they threw `FormatException`). Date/boolean cells are read with `GetDateTime()`/`GetBoolean()`, so results no longer depend on the current culture.
- All three read methods map headers by their actual column number: blank header cells no longer shift values into the wrong column (previously every column after a blank header cell was misaligned).
- Sheet-not-found behavior is inconsistent across `IExcelService` methods (kept as-is for backward compatibility): `ReadAs<T>` returns an **empty** sequence if `sheetName` isn't in the workbook, while `ReadAsDataTable` and `ReadAsObjects` resolve the sheet via `workbook.Worksheet(sheetName)` internally, which **throws** if the sheet doesn't exist.
- `ExcelService.Export` only wraps data in a one-row sheet when it's neither a `DataTable` nor `IEnumerable` (a `string` is treated as a single value, not as a sequence of characters) — passing a single plain object (not a list) still produces a one-record sheet, with a header row generated from its public properties.
- `CsvService.WriteAsync(DataTable)` writes each `DataRow.ItemArray` value via `csv.WriteField(item)` (typed `object`); no explicit formatting is applied beyond CsvHelper's own conversion, all under `CultureInfo.InvariantCulture`.
- `CsvService.ReadAs<T>` and `Read<T>` eagerly materialize results to a `List<T>` before returning (`GetRecords<T>().ToList()`), because the underlying `CsvReader`/`StreamReader` is disposed at the end of the method — these are not deferred/streaming enumerations despite the `IEnumerable<T>` return type.

---

## Breaking / behavior changes

### Unreleased

CSV (`CsvService`):

- **Formula-injection escaping is on by default** (`InjectionOptions.Escape`): on write, text fields starting with `=`, `@`, `+`, `-`, tab or CR get a leading `'` (plain numbers such as `-5` are not escaped; a tab-prefixed number such as `"\t5"` is). Opt out with `InjectionOptions.None` / `AddFileGenerator(o => o.CsvInjectionOptions = InjectionOptions.None)`.
- **Reads strip the escaping quote:** in `Escape` mode all `Read*` methods (headers included) remove one leading `'` from fields that start with `'` (one or more) followed by one of those characters — a foreign `'=x` is now read as `=x`.
- `ObjectConverter` (for `object` properties) parses with `CultureInfo.InvariantCulture` and tries `decimal` before `double`: `"3.14"` now comes back as `decimal` (previously `double`, current culture).
- The `Stream` overloads no longer close the caller's stream.

Excel (`ExcelService`):

- `ReadAs<T>` conversion errors name the cell: bad text for an enum now throws `FormatException` (was `ArgumentException`); non-convertible values throw `FormatException` (was `InvalidCastException`/`FormatException`); out-of-range values throw `OverflowException` naming the cell; a fractional number into an integral or enum property throws `FormatException` instead of being converted.
- `ReadAs<T>` converts from the cell's typed value with `CultureInfo.InvariantCulture` (was the current culture); blank cells leave non-nullable value-type properties at their default (previously a conversion exception).
- Duplicate header text: the first column wins in both `ReadAs<T>` (previously `InvalidOperationException`) and `ReadAsObjects` (previously the last column overwrote it).
- `ReadAsObjects` returns fractional numbers as `double` (previously `FormatException`) and reads date/boolean cells independent of the current culture.
- All read methods map by actual column number: blank header cells no longer shift later columns.
- `Export` treats a `string` as a single value, not as a sequence of characters.
