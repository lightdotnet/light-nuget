# Lightsoft.FileGenerator

CSV and Excel read/write helpers behind two small service interfaces — `ICsvService` (built on CsvHelper) and `IExcelService` (built on ClosedXML) — plus a DI extension to register both.

- **NuGet package id / assembly name:** `Lightsoft.FileGenerator` (no explicit `PackageId`, so it defaults to `AssemblyName`)
- **Root namespace:** `Light.FileGenerator` — types live under `Light.FileGenerator.File.Csv`, `Light.FileGenerator.File.Excel`, `Light.FileGenerator.Infrastructure.Csv`, `Light.FileGenerator.Infrastructure.Excel`, and `Light.Extensions.DependencyInjection` (the DI extension does not follow the root namespace)
- **Target framework:** netstandard2.1
- **Dependencies:** `ClosedXML` (`0.*`), `CsvHelper` (`33.*`), `Microsoft.Extensions.DependencyInjection.Abstractions`. No `ProjectReference`s — this is a leaf project.

## What's in this package

| Type | Namespace | Purpose |
|---|---|---|
| `ICsvService` | `Light.FileGenerator.File.Csv` | Contract for reading CSV headers/records (typed or dictionary-based) and writing an `IEnumerable<T>` or `DataTable` back out as a CSV stream. |
| `CsvData<T>` | `Light.FileGenerator.File.Csv` | DTO returned by `Read<T>`: `Headers` (`string[]`) + `Rows` (`IEnumerable<T>`). |
| `DictionaryData` | `Light.FileGenerator.File.Csv` | `CsvData<IDictionary<string, object?>>` — the untyped row shape returned by the non-generic `Read` overloads. |
| `IExcelService` | `Light.FileGenerator.File.Excel` | Contract for exporting one or more sheets (objects or `DataTable`s) to an `.xlsx` stream, and reading an `.xlsx` stream back as a `DataTable`, typed objects, or loose dictionaries. |
| `Worksheet` | `Light.FileGenerator.File.Excel` | Simple DTO pairing `Data` (`object`) with an optional `SheetName`, used by the `ExcelExtensions.Export(Worksheet[])` overload. |
| `ColumnOptions<T>` | `Light.FileGenerator.File.Excel` | Fluent builder mapping a `T` property (via a member-access expression) to an explicit Excel column header, for use with `IExcelService.ReadAs<T>`. |
| `ExcelExtensions` | `Light.FileGenerator.File.Excel` | Static sugar over `IExcelService.Export`: a single-list `Export<T>(list, sheetName)` overload and an `Export(params Worksheet[])` overload. |
| `CsvService` | `Light.FileGenerator.Infrastructure.Csv` | `ICsvService` implementation built on CsvHelper, pinned to `CultureInfo.InvariantCulture`. |
| `ObjectConverter` | `Light.FileGenerator.Infrastructure.Csv` | CsvHelper `DefaultTypeConverter` registered for `object`-typed properties; sniffs a raw field string into `long`/`double`/`decimal`/`bool`/`DateTime`, else leaves it as `string`. |
| `ExcelService` | `Light.FileGenerator.Infrastructure.Excel` | `IExcelService` implementation built on ClosedXML (`XLWorkbook`). |
| `Extensions` (internal) | `Light.FileGenerator.Infrastructure.Excel` | Internal helpers used by `ExcelService`: workbook → stream, worksheet resolution, header extraction, and value conversion for `ReadAs<T>`. |
| `ServiceCollectionExtensions` | `Light.Extensions.DependencyInjection` | `AddFileGenerator()` — registers both services. |

## `ICsvService` / `CsvService`

`CsvService` configures CsvHelper with `HasHeaderRecord = true`, `HeaderValidated = null` (missing headers don't throw) and `MissingFieldFound = null` (missing fields in a row don't throw) — reading is intentionally lenient. All reading/writing uses `CultureInfo.InvariantCulture`.

- `ReadHeaders(StreamReader | Stream)` — reads only the header row, returns `string[]?`.
- `ReadAs<T>(StreamReader | Stream)` — maps every row directly to `T` via `CsvReader.GetRecords<T>()`. Registers `ObjectConverter` so any `object`-typed property on `T` gets type-sniffed from its raw text.
- `Read<T>(StreamReader | Stream)` — same mapping as `ReadAs<T>`, but also returns the header row, wrapped in `CsvData<T>`; returns `null` if no header record was found.
- `Read(StreamReader | Stream)` — non-generic; returns `DictionaryData` (headers + one `Dictionary<string, object?>` per row, keyed by header name), or `null` if no headers.
- `WriteAsync<T>(IEnumerable<T> records, bool excludeHeader = false)` — writes a header row (unless `excludeHeader`) then each record, returning a rewound `MemoryStream`.
- `WriteAsync(DataTable table, bool excludeHeader = false)` — same, but source is a `DataTable`: writes each `DataColumn.ColumnName` as the header, then each `DataRow.ItemArray` value via `csv.WriteField`.

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
- `ReadAs<T>(Stream streamData, string? sheetName = null, ColumnOptions<T>? options = null)` — maps each row to a new `T` (via `Activator.CreateInstance`, so `T` needs a public parameterless constructor), matching columns to public properties by name unless overridden with `ColumnOptions<T>.SetColumn`. If `sheetName` is supplied but not present in the workbook, this returns an **empty** sequence rather than throwing.
- `ReadAsObjects(Stream streamData, string? sheetName = null)` — maps each row to a loose `Dictionary<string, object>`, converting numeric cells with `Convert.ToInt64`, date cells with `Convert.ToDateTime`, boolean cells with `Convert.ToBoolean`, and everything else via `.ToString()`.

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
```

`AddFileGenerator()` (in `Light.Extensions.DependencyInjection`) registers `ICsvService -> CsvService` and `IExcelService -> ExcelService`, both `AddTransient`. There are no options to configure.

## Notes

- `ObjectConverter.ConvertFromString` checks types in this order: `long` → `double` → `decimal` → `bool` → `DateTime`. Because `double.TryParse` already succeeds for ordinary fractional literals (e.g. `"3.14"`), the `decimal` branch is effectively unreachable in practice — expect `object`-typed CSV values to come back as `double`, not `decimal`, in the common case.
- `ICsvService.Read(StreamReader | Stream)` (the non-generic, `DictionaryData`-returning overload) registers `ObjectConverter` on the reader but never actually uses it: rows are populated via `csv.GetField(header)`, which returns raw text. So `DictionaryData.Rows` values are always `string`, unlike `ReadAs<T>`/`Read<T>`, where `object`-typed target properties are converted.
- `IExcelService.ReadAsObjects` converts every numeric cell with `Convert.ToInt64(cell.Value.ToString())`. Verified by test: this does **not** truncate/round a fractional value — `Convert.ToInt64(string)` requires an exact integer string, so a cell like `3.7` throws `FormatException` instead. Use `ReadAsDataTable` or `ReadAs<T>` if the sheet may contain non-integer numeric values.
- Sheet-not-found behavior is inconsistent across `IExcelService` methods: `ReadAs<T>` returns an **empty** sequence if `sheetName` isn't in the workbook, while `ReadAsDataTable` and `ReadAsObjects` resolve the sheet via `workbook.Worksheet(sheetName)` internally, which **throws** if the sheet doesn't exist.
- `ExcelService.Export` only wraps data in a one-row sheet when it's neither a `DataTable` nor `IEnumerable` — passing a single plain object (not a list) still produces a one-record sheet, with a header row generated from its public properties.
- `CsvService.WriteAsync(DataTable)` writes each `DataRow.ItemArray` value via `csv.WriteField(item)` (typed `object`); no explicit formatting is applied beyond CsvHelper's own conversion, all under `CultureInfo.InvariantCulture`.
- `CsvService.ReadAs<T>` and `Read<T>` eagerly materialize results to a `List<T>` before returning (`GetRecords<T>().ToList()`), because the underlying `CsvReader`/`StreamReader` is disposed at the end of the method — these are not deferred/streaming enumerations despite the `IEnumerable<T>` return type.
