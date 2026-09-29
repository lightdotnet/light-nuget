[← Back to main README](https://github.com/lightdotnet/light-nuget#readme)

# WebApi (Plugins Sample)

A runnable ASP.NET Core Web API sample under `src/plugins/samples/WebApi`. It is **not** a NuGet package — it exists to exercise every library in the `Plugins` solution (`src/plugins`) from real `Program.cs` startup code and real controllers, so a developer can see each package wired up and called in context.

`WebApi.csproj` (`net10.0`) has a `ProjectReference` to all five libraries in `src/plugins/src`:

| Library | Used by |
|---|---|
| `ActiveDirectory` | `ADController` |
| `FileGenerator` | `CsvController`, `ExcelController` |
| `Graph` | `GraphController` (registered only when `Graph` credentials are configured — see below) |
| `Serilog` | wired globally via `ConfigureSerilog()` in `Program.cs` |
| `SmtpMail` | `MailController` |

It also references `Lightsoft.Extensions` (NuGet) and `Swashbuckle.AspNetCore.SwaggerGen`/`SwaggerUI` directly.

## What's wired up in `Program.cs`

| Call | Status | Notes |
|---|---|---|
| `builder.Host.ConfigureSerilog()` | active | From the `Serilog` package; reads the `Serilog` section of `appsettings.json`. |
| `builder.Services.AddHostedService<Worker>()` | commented out | No `Worker` class exists in the project — this line would not compile if uncommented as-is. |
| `builder.Services.AddActiveDirectory(opt => opt.Name = "company.local")` | active | The **real** `ActiveDirectoryService` overload (Windows-only, `System.DirectoryServices.AccountManagement`), not the no-op `FakeActiveDirectoryService`. Wrapped in `#pragma warning disable CA1416` because the API is `[SupportedOSPlatform("windows")]`. |
| `builder.Services.AddMicrosoftGraph(...)` | conditional | Registered only when `Graph:TenantId`, `Graph:ClientId` and `Graph:ClientSecret` are all non-empty in configuration (empty placeholders in `appsettings.json`; supply real values via `dotnet user-secrets`/environment variables). When not registered, `GraphController` resolves the Graph services optionally and returns `503 Service Unavailable` instead of failing DI. |
| `builder.Services.AddFileGenerator()` | active | Registers `ICsvService`/`IExcelService` (both `AddTransient`) used by `CsvController`/`ExcelController`. |
| `builder.Services.AddControllers(...)` with a custom `ByteArrayModelBinderProvider` | active | Lets `byte[]`-bodied actions (e.g. `ExcelController.Import([FromBody] byte[])`) bind from either a Swagger multipart file picker or a raw JSON base64 string body — see `ByteArrayFileUploadFilter.cs`. |
| `builder.Services.AddSwaggerGen(c => c.OperationFilter<RawByteArrayBodyFilter>())` | active | Makes Swagger render `byte[]` request bodies as a file-upload widget instead of a base64 string field. |
| `app.UseSwagger()` / `app.UseSwaggerUI()` | active | Swagger UI is enabled with no environment guard (runs in all environments as configured today). |
| `app.UseAuthentication()` / `app.UseAuthorization()` | active | No authentication scheme is registered, so these currently run as a no-op pass-through — no `[Authorize]` attributes are used anywhere in the sample. |

`SmtpMail` is **not** registered via DI at all — `MailController` constructs `SmtpMailKitSender` directly inside the action method (see below).

## Controllers

| Controller | Route(s) | Method | Demonstrates |
|---|---|---|---|
| `ADController` | `GET /AD?user={user}` | GET | `IActiveDirectoryService.GetByUserNameAsync` — look up an AD user by username. |
| | `GET /AD/check_password?user={user}&password={password}` | GET | `IActiveDirectoryService.CheckPasswordSignInAsync` — validate AD credentials. |
| `CsvController` | `GET /Csv/read?fileName={name}` | GET | `ICsvService.Read(TextReader)` — reads `{Csv:FilesDirectory}/{fileName}.csv` into a `DataTable` (only the bare file name is used, so `../` can't escape the folder; `404` if missing). |
| | `GET /Csv/read_as?fileName={name}` | GET | `ICsvService.Read<CsvObject>(TextReader)` — reads the same file into a typed `CsvObject` (via `CsvHelper` `[Index]` attributes). |
| | `GET /Csv/export` | GET | `ICsvService.ReadAs<T>` + `WriteAsync<T>` — reads `{Csv:FilesDirectory}/{Csv:ExportSourceFile}`, round-trips it through the service, and returns it as a downloadable `DataExport.csv`. |
| | `GET /Csv/export_dt` | GET | `ICsvService.WriteAsync(DataTable)` — builds an in-memory `DataTable` (including a row with a missing `Id` and one with a missing `Name`) and exports it as `DataTableCsvExport.csv`. |
| `ExcelController` | `GET /Excel` | GET | `IExcelService.Export((object List, string? SheetName))` — exports an in-memory `List<object>` (anonymous types) as `DataExport.xlsx`. |
| | `GET /Excel/import` | GET | `IExcelService.ReadAsDataTable(Stream)` — reads a hardcoded local `.xlsx` file and converts rows via `Light.Extensions`' `DataTableHelper.ConvertToObjects`. |
| | `POST /Excel/upload` | POST | Same import path as above, but the workbook bytes come from the request body (`byte[]`) via the custom `ByteArrayModelBinderProvider`/Swagger file picker instead of a local path. |
| | `GET /Excel/test` | GET | Not related to `FileGenerator` — just an indexed `Select` LINQ loop that writes to `Console.WriteLine`; returns `200 OK` with no body. |
| | `GET /Excel/export_multi_list` | GET | `IExcelService.Export(params (object Data, string? Name)[])` — exports three different in-memory objects (two lists, one plain object) as separate sheets in one workbook. |
| | `GET /Excel/export_multi_dt` | GET | Same multi-sheet `Export` overload, but with two `DataTable`s instead of lists/objects. |
| `GraphController` | `GET /Graph/send_email` | GET | `IGraphMailService.SendAsync` — sends mail via Microsoft Graph, called with hardcoded primitive arguments (`from`/`recipients`/`subject`/`content` all literal strings in the controller). |
| | `GET /Graph?user={user}` | GET | `IGraphTeams.GetChatsAsync` — lists a user's Teams chats. |
| `MailController` | `GET /Mail` | GET | Constructs `SmtpMail`'s `SmtpMailKitSender` directly (not via DI) with `UseSsl = false`, then calls `SendAsync(from, fromDisplayName, recipients, subject, content)` with hardcoded literal arguments. |

## Setup required to actually run each endpoint

- **`GraphController`** — set `Graph:TenantId`, `Graph:ClientId` and `Graph:ClientSecret` for a real Azure AD app registration (with `Mail.Send`/`Chat.Read.All` application permissions, admin-consented). Until then its endpoints return `503`.
- **`ADController`** — needs a reachable, domain-joined Active Directory / Windows domain matching the configured domain name `"company.local"` (`Program.cs`). This only works on Windows (`[SupportedOSPlatform("windows")]`) and against a real directory service; there is no fake/mock wired up for this sample.
- **`CsvController`** — `/Csv/read` and `/Csv/read_as` read `{fileName}.csv` from `Csv:FilesDirectory` (default `Files`, relative to the content root; an absolute path also works); `/Csv/export` reads `Csv:ExportSourceFile` (default `export-source.csv`) from the same folder. Missing files return `404`. `/Csv/export_dt` needs no external file.
- **`ExcelController`** — `/Excel/import` requires a file at `D:\test.xlsx`. `/Excel/upload` needs no local file (bytes come from the request body). `/Excel`, `/Excel/test`, `/Excel/export_multi_list`, and `/Excel/export_multi_dt` need no external file.
- **`MailController`** — reads SMTP `Host`/`UserName`/`Password` from the `SMTP` configuration section (placeholders in `appsettings.json`). Supply real values via `dotnet user-secrets` or environment variables; never commit them.
- **Configuration secrets in general** — `appsettings.json` contains only placeholders (`<your_password>`, `<your-elastic-password>`, empty `Graph` values). Supply real values via `dotnet user-secrets`, environment variables or a vault — never commit them.

## Running it

From this folder:

```
dotnet run
```

Swagger UI is enabled unconditionally (`app.UseSwagger()` / `app.UseSwaggerUI()`, no environment check), so once the app is running, browse to `/swagger` to see and try every endpoint listed above. Endpoints that depend on external systems (AD, Redis, local files, Graph) will return errors unless the corresponding setup note above has been satisfied.
