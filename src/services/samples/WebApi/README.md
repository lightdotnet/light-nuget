[← Back to main README](https://github.com/lightdotnet/light-nuget#readme)

# WebApi (IntegrationServices Sample)

A runnable ASP.NET Core Web API sample under `src/services/samples/WebApi`. It is **not** a NuGet package — it exists to exercise every library in the `IntegrationServices` solution (`src/services`) from real `Program.cs` startup code and real controllers, so a developer can see each package wired up and called in context.

`WebApi.csproj` (`net10.0`) has a `ProjectReference` to all six libraries in `src/services/src`:

| Library | Used by |
|---|---|
| `ActiveDirectory` | `ADController` |
| `FileGenerator` | `CsvController`, `ExcelController` |
| `Graph` | `GraphController` (registration currently disabled — see below) |
| `Mail.Contracts` | `MailController`, `GraphController` (shared `MailFrom`/`MailMessage` types) |
| `Serilog` | wired globally via `ConfigureSerilog()` in `Program.cs` |
| `SmtpMail` | `MailController` |

It also references `Lightsoft.Extensions` (NuGet) and `Swashbuckle.AspNetCore.SwaggerGen`/`SwaggerUI` directly.

## What's wired up in `Program.cs`

| Call | Status | Notes |
|---|---|---|
| `builder.Host.ConfigureSerilog()` | active | From the `Serilog` package; reads the `Serilog` section of `appsettings.json`. |
| `builder.Services.AddHostedService<Worker>()` | commented out | No `Worker` class exists in the project — this line would not compile if uncommented as-is. |
| `builder.Services.AddActiveDirectory(opt => opt.Name = "company.local")` | active | The **real** `ActiveDirectoryService` overload (Windows-only, `System.DirectoryServices.AccountManagement`), not the no-op `FakeActiveDirectoryService`. Wrapped in `#pragma warning disable CA1416` because the API is `[SupportedOSPlatform("windows")]`. |
| `builder.Services.AddMicrosoftGraph(opt => { ClientSecret/ClientId/TenantId = "" })` | commented out | Disabled because it needs real Azure AD app registration credentials. `GraphController` still exists and takes a dependency on `IGraphMailService`/`IGraphTeams`, so calling its endpoints while this is commented out will fail with a DI resolution error. |
| `builder.Services.AddFileGenerator()` | active | Registers `ICsvService`/`IExcelService` (both `AddTransient`) used by `CsvController`/`ExcelController`. |
| `builder.Services.AddControllers(...)` with a custom `ByteArrayModelBinderProvider` | active | Lets `byte[]`-bodied actions (e.g. `ExcelController.Import([FromBody] byte[])`) bind from either a Swagger multipart file picker or a raw JSON base64 string body — see `ByteArrayFileUploadFilter.cs`. |
| `builder.Services.AddSwaggerGen(c => c.OperationFilter<RawByteArrayBodyFilter>())` | active | Makes Swagger render `byte[]` request bodies as a file-upload widget instead of a base64 string field. |
| `app.UseSwagger()` / `app.UseSwaggerUI()` | active | Swagger UI is enabled with no environment guard (runs in all environments as configured today). |
| `app.UseAuthentication()` / `app.UseAuthorization()` | active | No authentication scheme is registered, so these currently run as a no-op pass-through — no `[Authorize]` attributes are used anywhere in the sample. |

`SmtpMail` and `Mail.Contracts` are **not** registered via DI at all — `MailController` constructs `SmtpMailKit` directly inside the action method (see below).

## Controllers

| Controller | Route(s) | Method | Demonstrates |
|---|---|---|---|
| `ADController` | `GET /AD?user={user}` | GET | `IActiveDirectoryService.GetByUserNameAsync` — look up an AD user by username. |
| | `GET /AD/check_password?user={user}&password={password}` | GET | `IActiveDirectoryService.CheckPasswordSignInAsync` — validate AD credentials. |
| `CsvController` | `GET /Csv/read?fileName={name}` | GET | `ICsvService.Read(TextReader)` — reads `D:\Files\{fileName}.csv` into a `DataTable`. |
| | `GET /Csv/read_as?fileName={name}` | GET | `ICsvService.Read<CsvObject>(TextReader)` — reads the same file into a typed `CsvObject` (via `CsvHelper` `[Index]` attributes). |
| | `GET /Csv/export` | GET | `ICsvService.ReadAs<T>` + `WriteAsync<T>` — reads a hardcoded local CSV, round-trips it through the service, and returns it as a downloadable `DataExport.csv`. |
| | `GET /Csv/export_dt` | GET | `ICsvService.WriteAsync(DataTable)` — builds an in-memory `DataTable` (including a row with a missing `Id` and one with a missing `Name`) and exports it as `DataTableCsvExport.csv`. |
| `ExcelController` | `GET /Excel` | GET | `IExcelService.Export((object List, string? SheetName))` — exports an in-memory `List<object>` (anonymous types) as `DataExport.xlsx`. |
| | `GET /Excel/import` | GET | `IExcelService.ReadAsDataTable(Stream)` — reads a hardcoded local `.xlsx` file and converts rows via `Light.Extensions`' `DataTableHelper.ConvertToObjects`. |
| | `POST /Excel/upload` | POST | Same import path as above, but the workbook bytes come from the request body (`byte[]`) via the custom `ByteArrayModelBinderProvider`/Swagger file picker instead of a local path. |
| | `GET /Excel/test` | GET | Not related to `FileGenerator` — just an indexed `Select` LINQ loop that writes to `Console.WriteLine`; returns `200 OK` with no body. |
| | `GET /Excel/export_multi_list` | GET | `IExcelService.Export(params (object Data, string? Name)[])` — exports three different in-memory objects (two lists, one plain object) as separate sheets in one workbook. |
| | `GET /Excel/export_multi_dt` | GET | Same multi-sheet `Export` overload, but with two `DataTable`s instead of lists/objects. |
| `GraphController` | `GET /Graph/send_email` | GET | `IGraphMailService.SendAsync` — sends mail via Microsoft Graph using `Mail.Contracts`' `MailFrom`/`MailMessage` types. |
| | `GET /Graph?user={user}` | GET | `IGraphTeams.GetChatsAsync` — lists a user's Teams chats. |
| `MailController` | `GET /Mail` | GET | Sends an email through `SmtpMail`'s `SmtpMailKit`, constructed directly in the action (not via DI) with `UseSsl = false`. Attachment/CC/BCC code paths exist but are commented out. |

## Setup required to actually run each endpoint

- **`GraphController`** — will fail with a DI resolution error for `IGraphMailService`/`IGraphTeams` as the code stands today, because `AddMicrosoftGraph` is commented out in `Program.cs`. To make it work you must uncomment that block and supply a real Azure AD app registration's `TenantId`, `ClientId`, and `ClientSecret` (currently empty strings in the commented-out code).
- **`ADController`** — needs a reachable, domain-joined Active Directory / Windows domain matching the configured domain name `"company.local"` (`Program.cs`). This only works on Windows (`[SupportedOSPlatform("windows")]`) and against a real directory service; there is no fake/mock wired up for this sample.
- **`CsvController`** — `/Csv/read` and `/Csv/read_as` require a file at `D:\Files\{fileName}.csv`; `/Csv/export` requires the hardcoded file `D:\Files\Adobe_aswDM50210_20250311182339.csv` to exist. `/Csv/export_dt` needs no external file.
- **`ExcelController`** — `/Excel/import` requires a file at `D:\test.xlsx`. `/Excel/upload` needs no local file (bytes come from the request body). `/Excel`, `/Excel/test`, `/Excel/export_multi_list`, and `/Excel/export_multi_dt` need no external file.
- **`MailController`** — **security note:** `Controllers/MailController.cs` has real-looking Ethereal (fake/test SMTP) credentials hardcoded directly in source (`host = "smtp.ethereal.email"`, `userName = "jermain.torphy@ethereal.email"`, `password = "GHMdV12nF7zfFhqG7Z"`) and committed to the repository. Ethereal is a disposable test-inbox service (no real mail is delivered), so the practical blast radius is limited, but hardcoded credentials committed to source control should still be treated as a smell — do not copy this pattern into production code, and rotate/replace these values if this sample is ever adapted for real use.
- **Configuration secrets in general** — `appsettings.json` also has plaintext credentials checked into source: an `SMTP` section with a Gmail account (`zord.contactus@gmail.com`) and app password, and a `Serilog` → `ElasticsearchAsync1` sink with `Username`/`Password` `elastic`/`elastic` against an internal endpoint. None of these are read via user secrets/environment variables/a vault — treat this file as sample-only and never commit real credentials to it in a fork of this pattern.

## Running it

From this folder:

```
dotnet run
```

Swagger UI is enabled unconditionally (`app.UseSwagger()` / `app.UseSwaggerUI()`, no environment check), so once the app is running, browse to `/swagger` to see and try every endpoint listed above. Endpoints that depend on external systems (AD, Redis, local files, Graph) will return errors unless the corresponding setup note above has been satisfied.
