<img align="left" width="116" height="116" src="https://github.com/lightdotnet/light-nuget/blob/main/.github/light-dotnet.png" />
<br/>

# Light .NET Packages

## About
This solution contains Light .NET projects publish to NuGet.org

## Packages

### EventBus

- [EventBus](src/eventbus/README.md) — Transport-agnostic `IEventBus` contract, plus a MassTransit + RabbitMQ implementation

### Framework

- [Lightsoft.SharedKernel](src/framework/src/SharedKernel/README.md) — DDD-ish entity/value-object base types, a string ID generator, typed HTTP exceptions, and dynamic-column-to-POCO mapping helpers
- [Lightsoft.AspNetCore.Modularity](src/framework/src/Modularity/README.md) — Module system for composing an ASP.NET Core app out of self-contained modules, plus convention-based DI auto-registration
- [Lightsoft.AspNetCore.Authorization](src/framework/src/Authorization/README.md) — Permission-based authorization building blocks — use `[Authorize(Policy = "...")]` as permission checks without pre-registering every policy
- [Lightsoft.AspNetCore.Swagger](src/framework/src/Swagger/README.md) — Configuration-driven Swagger/OpenAPI + Swagger UI setup with optional JWT/Basic auth and API-version awareness
- [Lightsoft.Extensions](src/framework/src/Extensions/README.md) — Static helper/extension classes for argument guards, date/time, random generation, string/number conversion, reflection, JSON, streams, XML, query strings, and enums
- [Lightsoft.AspNetCore.Extensions](src/framework/src/WebHost/README.md) — Default hosting config: JWT auth setup, request logging, exception handling, JSON converters, MVC conventions/base controllers, and config-loading helpers
- [Lightsoft.Specification](src/framework/src/Specification/README.md) — Specification Pattern with context-agnostic Repository + Unit of Work interfaces
- [Lightsoft.EntityFrameworkCore](src/framework/src/EntityFrameworkCore/README.md) — EF Core implementations of Specification's Repository/Unit of Work, plus NOLOCK, Dapper, and global-query-filter extensions
- [Lightsoft.Caching](src/framework/src/Caching/README.md) — Swappable in-process/Redis cache behind a single `ICacheService` interface

### Plugins

- [Lightsoft.ActiveDirectory](src/plugins/src/ActiveDirectory/README.md) — Pluggable AD/LDAP authentication and user lookup behind a single `IActiveDirectoryService` abstraction
- [Lightsoft.FileGenerator](src/plugins/src/FileGenerator/README.md) — CSV/Excel read-write helpers (`ICsvService`, `IExcelService`)
- [Lightsoft.Graph](src/plugins/src/Graph/README.md) — Thin Microsoft Graph SDK wrapper for sending mail and reading Teams chats (app-only auth)
- [Lightsoft.Serilog](src/plugins/src/Serilog/README.md) — Serilog wiring for ASP.NET Core Generic Host apps (console/file/Elasticsearch sinks, enrichers)
- [Lightsoft.SmtpMail](src/plugins/src/SmtpMail/README.md) — SMTP email senders (`SmtpClient` + MailKit) behind a shared `ISmtpMailSender` contract

### Blazor

- [Blazor Extra Components](src/blazor/README.md) — Reusable Blazor components (early stage)
