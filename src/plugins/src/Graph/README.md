[← Back to main README](https://github.com/lightdotnet/light-nuget#readme)

# Lightsoft.Graph

Thin wrapper around the Microsoft Graph SDK for sending mail and reading Teams chats as an application (app-only, client-credentials auth). Authentication is handled internally via `Azure.Identity`'s `ClientSecretCredential`; consumers only supply a tenant/client/secret triple and a `GraphServiceClient` is registered for them.

- **NuGet package id / assembly name:** `Lightsoft.Graph` (`AssemblyName` is explicitly set; no `PackageId`, so it defaults to `AssemblyName`)
- **Root namespace:** the public contracts (`IGraphMailService`, `IGraphTeams`) live under `Light.Graph`; the implementations (`GraphMailService`, `GraphTeamsService`, `GraphOptions`) live under `Light.Infrastructure`; DI registration lives under `Light.Extensions.DependencyInjection`
- **Target framework:** netstandard2.1
- **Dependencies:** `Azure.Identity` (`1.*`), `Microsoft.Graph` (`6.*`). No project references — the package no longer depends on a shared `Mail.Contracts` DTO project; `IGraphMailService.SendAsync` takes plain primitive parameters directly.

## What's in this package

| Type | Namespace | Purpose |
|---|---|---|
| `IGraphMailService` | `Light.Graph` | `Task SendAsync(string from, List<string> recipients, string subject, string content, List<string>? ccRecipients = null, List<string>? bccRecipients = null, Dictionary<string, byte[]>? attachments = null, CancellationToken cancellationToken = default)` — sends mail as a given user via Graph. |
| `GraphMailService` | `Light.Infrastructure` | Public implementation of `IGraphMailService`, backed by a `GraphServiceClient`. |
| `IGraphTeams` | `Light.Graph` | `Task<ChatCollectionResponse?> GetChatsAsync(string user)` — lists a user's Teams chats. |
| `GraphTeamsService` | `Light.Infrastructure` | **Internal** implementation of `IGraphTeams`, backed by a `GraphServiceClient`. Not directly constructible by consumers — resolve it through `IGraphTeams`. |
| `GraphOptions` | `Light.Infrastructure` | Options bag: `TenantId`, `ClientId`, `ClientSecret` (all `string?`). |
| `ServiceCollectionExtensions` | `Light.Extensions.DependencyInjection` | `AddMicrosoftGraph(Action<GraphOptions> action)` registration helper. |

## `GraphMailService.SendAsync` behavior

Builds a Graph `Message` from the supplied parameters:

- `ToRecipients` — from `recipients` (required).
- `Subject` / `Body` — `Body.ContentType` is always `BodyType.Html`, populated from `content`.
- `CcRecipients` / `BccRecipients` — set only if `ccRecipients` / `bccRecipients` is non-null.
- `Attachments` — set only if `attachments` is non-null; each `Dictionary<string, byte[]>` entry becomes a Graph file attachment (`OdataType = "#microsoft.graph.fileAttachment"`) named after the key, with `AdditionalData["contentBytes"]` set to `Convert.ToBase64String(value)`.

The message is sent via `_graphServiceClient.Users[from].SendMail.PostAsync(...)` with `SaveToSentItems = true` — i.e. it is sent **as** the mailbox identified by the `from` address (app-only Graph auth requires `Mail.Send` application permission and, typically, an application access policy scoping which mailboxes it can send as).

## `GraphTeamsService.GetChatsAsync` behavior

Calls `_graphServiceClient.Users[user].Chats.GetAsync()` and returns the raw `Microsoft.Graph.Models.ChatCollectionResponse?` (a single page — no automatic paging is performed).

## Usage

### 1. Register services

```csharp
builder.Services.AddMicrosoftGraph(opt =>
{
    opt.TenantId = builder.Configuration["Graph:TenantId"];
    opt.ClientId = builder.Configuration["Graph:ClientId"];
    opt.ClientSecret = builder.Configuration["Graph:ClientSecret"];
});
```

`AddMicrosoftGraph` builds a `ClientSecretCredential` (scoped to `AzureAuthorityHosts.AzurePublicCloud`) from the bound `GraphOptions`, registers a single `GraphServiceClient` as `AddSingleton`, `IGraphMailService` as `AddScoped`, and `IGraphTeams` as `AddTransient`.

### 2. Send mail

```csharp
public class NotificationController(IGraphMailService graphMailService) : ControllerBase
{
    [HttpPost("send")]
    public async Task<IActionResult> Send(CancellationToken cancellationToken)
    {
        await graphMailService.SendAsync(
            from: "notifications@contoso.com",
            recipients: ["someone@contoso.com"],
            subject: "Hello",
            content: "<p>Hello from Graph.</p>",
            cancellationToken: cancellationToken);

        return Ok();
    }
}
```

### 3. List a user's Teams chats

```csharp
public class TeamsController(IGraphTeams graphTeams) : ControllerBase
{
    [HttpGet("chats")]
    public async Task<IActionResult> GetChats(string user)
    {
        var chats = await graphTeams.GetChatsAsync(user);

        return Ok(chats);
    }
}
```

## Notes

- Both service lifetimes wrap a single, application-wide `GraphServiceClient` (`AddSingleton`) — the `AddScoped`/`AddTransient` registrations for `IGraphMailService`/`IGraphTeams` don't create new Graph connections per request, they just wrap the shared client in a thin, stateless service.
- `GraphTeamsService` is `internal`; the only supported entry point is `IGraphTeams`, resolved through DI.
- `GetChatsAsync` was renamed and retyped from an earlier `GetByAsync(string user) : Task<object?>` signature to `GetChatsAsync(string user) : Task<ChatCollectionResponse?>` — callers pattern-matching on `object` will need to update to the concrete `Microsoft.Graph.Models.ChatCollectionResponse` type.
- `SendAsync` has no built-in retry/throttling handling beyond whatever the underlying `Microsoft.Graph` SDK's default request adapter does; transient Graph errors (e.g. `429`) propagate as exceptions.
- All Graph calls run under app-only (client-credentials) permissions — there is no delegated/user-token flow in this package, and no interactive consent step; required Graph application permissions (e.g. `Mail.Send`, `Chat.Read.All`) must be granted and admin-consented on the Azure AD app registration ahead of time.
