[← Back to main README](https://github.com/lightdotnet/light-nuget#readme)

# Lightsoft.SmtpMail

SMTP email-sending implementations behind a single shared contract, `ISmtpMailSender`. The package ships two senders — one built on the legacy `System.Net.Mail.SmtpClient` (no authentication) and one built on MailKit (with authentication) — plus optional `IServiceCollection` registration helpers for each.

- **NuGet package id / assembly name:** `Lightsoft.SmtpMail` (no explicit `PackageId`, so it defaults to `AssemblyName`)
- **Root namespace:** `Light.Smtp` — the interface, both senders, and the options classes all live directly under `Light.Smtp` (source files sit under `Smtp/`); DI extensions live under `Light.Extensions.DependencyInjection`
- **Target framework:** netstandard2.1
- **Dependencies:** `MailKit` (4.17.0), `MimeKit` (4.17.0), `Microsoft.Extensions.DependencyInjection.Abstractions`. No project references — the package no longer depends on a shared `Mail.Contracts` DTO project; `ISmtpMailSender.SendAsync` takes plain primitive parameters directly.

## What's in this package

| Type | Namespace | Purpose |
|---|---|---|
| `ISmtpMailSender` | `Light.Smtp` | Shared contract: `Task SendAsync(string from, string fromDisplayName, List<string> recipients, string subject, string content, List<string>? cc = null, List<string>? bcc = null, Dictionary<string, byte[]>? attachments = null, CancellationToken cancellationToken = default)`. Implemented by both senders below. |
| `SmtpConnection` | `Light.Smtp` | Abstract base exposing `Host` (protected set), `Port` (protected set), and `UseSsl` (public set) — common connection state for both senders. |
| `SmtpNetMailSender` | `Light.Smtp` | `SmtpConnection` + `ISmtpMailSender` implementation using the built-in `System.Net.Mail.SmtpClient`. No authentication support. |
| `SmtpMailKitSender` | `Light.Smtp` | `SmtpConnection` + `ISmtpMailSender` implementation using MailKit's `SmtpClient`. Supports authentication (`UserName`/`Password`, both public get / protected set). |
| `SmtpMailOptions` | `Light.Smtp` | Options bag for `AddSmtpMail`: `Host`, `Port` (default `25`), `UseSsl` (default `false`). |
| `SmtpMailKitOptions` | `Light.Smtp` | Options bag for `AddSmtpMailKit`: `Host`, `Port` (default `587`), `UserName`, `Password`, `UseSsl` (default `false`). |
| `ServiceCollectionExtensions` | `Light.Extensions.DependencyInjection` | `AddSmtpMail(Action<SmtpMailOptions>)` and `AddSmtpMailKit(Action<SmtpMailKitOptions>)` — both register `ISmtpMailSender` as `AddTransient`. |

## `SmtpNetMailSender` vs `SmtpMailKitSender` — which one to use

- **`SmtpNetMailSender`** wraps `System.Net.Mail.SmtpClient` directly. It has no way to authenticate — it's only suitable for simple/local relays that accept anonymous connections (e.g. an internal relay, a dev SMTP catcher, IIS SMTP). Because the legacy `SmtpClient` has no cancellation-token-accepting `SendMailAsync` overload on netstandard2.1, cancellation is wired up by registering `smtpClient.SendAsyncCancel` against the `CancellationToken` — see [Notes](#notes) for the resulting behavior.
- **`SmtpMailKitSender`** wraps MailKit's `SmtpClient` and always authenticates (`ConnectAsync` → `AuthenticateAsync` → `SendAsync` → `DisconnectAsync`), all with full native `CancellationToken` support. This is the generally recommended choice for real mail providers (SendGrid, Ethereal, Gmail SMTP, Office 365, etc.) that require authenticated, TLS-capable SMTP.

In short: reach for `SmtpMailKitSender` unless you specifically need to talk to an unauthenticated relay, in which case `SmtpNetMailSender` is the lighter-weight option.

## Usage

### Dependency injection

```csharp
// Unauthenticated relay via System.Net.Mail.SmtpClient
services.AddSmtpMail(options =>
{
    options.Host = "localhost";
    options.Port = 25;
    options.UseSsl = false;
});

// Authenticated SMTP via MailKit
services.AddSmtpMailKit(options =>
{
    options.Host = "smtp.ethereal.email";
    options.Port = 587;
    options.UserName = "jermain.torphy@ethereal.email";
    options.Password = "GHMdV12nF7zfFhqG7Z";
    options.UseSsl = false;
});
```

Both extensions register `ISmtpMailSender` with `AddTransient`, so inject `ISmtpMailSender` and call `SendAsync`:

```csharp
public class NotificationService
{
    private readonly ISmtpMailSender _mailSender;

    public NotificationService(ISmtpMailSender mailSender) => _mailSender = mailSender;

    public Task NotifyAsync(CancellationToken cancellationToken) =>
        _mailSender.SendAsync(
            from: "no-reply@example.com",
            fromDisplayName: "Example App",
            recipients: ["user@example.com"],
            subject: "Welcome",
            content: "<p>Thanks for signing up.</p>",
            cancellationToken: cancellationToken);
}
```

Only register one of `AddSmtpMail`/`AddSmtpMailKit` per `ISmtpMailSender` consumer — calling both registers two implementations against the same service type, and the last one registered wins for a plain `ISmtpMailSender` injection.

### Direct instantiation (no DI)

Both senders can also be constructed directly, e.g. from a controller or background job:

```csharp
var smtpClient = new SmtpMailKitSender("smtp.ethereal.email", "jermain.torphy@ethereal.email", "GHMdV12nF7zfFhqG7Z")
{
    UseSsl = false
};

await smtpClient.SendAsync(
    from: "leslie.bailey@ethereal.email",
    fromDisplayName: "Leslie Bailey",
    recipients: ["test@yopmail.com"],
    subject: "Test " + DateTime.Now,
    content: "Hello, this is a test mail");
```

`SmtpNetMailSender` is constructed the same way, minus credentials:

```csharp
var smtpClient = new SmtpNetMailSender("localhost", 25) { UseSsl = false };

await smtpClient.SendAsync(
    from: "leslie.bailey@ethereal.email",
    fromDisplayName: "Leslie Bailey",
    recipients: ["test@yopmail.com"],
    subject: "Test",
    content: "Hello, this is a test mail");
```

### Attachments

Both senders accept an optional `Dictionary<string, byte[]>? attachments` parameter (key = file name, value = file bytes) on `SendAsync`:

```csharp
var attachments = new Dictionary<string, byte[]>
{
    ["report.pdf"] = fileBytes
};

await smtpClient.SendAsync(
    from: "leslie.bailey@ethereal.email",
    fromDisplayName: "Leslie Bailey",
    recipients: ["test@yopmail.com"],
    subject: "Report",
    content: "See attached.",
    attachments: attachments);
```

`SmtpNetMailSender` wraps each attachment's byte array in a `MemoryStream` and adds it as a `System.Net.Mail.Attachment`; `SmtpMailKitSender` adds each one to a MailKit `BodyBuilder.Attachments` collection via `bodyBuilder.Attachments.Add(name, bytes)`. `cc`/`bcc`/`attachments` are all optional (`null`-safe — skipped when `null`); `recipients` is not — it must be a non-null, non-empty list or `SendAsync` throws when iterating it.

## Notes

- Both senders always send an HTML body: `SmtpNetMailSender` sets `IsBodyHtml = true`, and `SmtpMailKitSender` sets `HtmlBody` on the `BodyBuilder`. Plain-text bodies aren't supported by either sender.
- `SmtpMailKitSender` always authenticates — there is no anonymous-send path on that class. If you need an unauthenticated relay, use `SmtpNetMailSender` instead.
- `SmtpNetMailSender`'s cancellation support is indirect: it registers `SmtpClient.SendAsyncCancel` against the token rather than passing the token into a native async overload (none exists for `SmtpClient.SendMailAsync` on netstandard2.1). Cancelling aborts the in-flight send, but the exception surfaced comes from `SmtpClient` itself and is not guaranteed to be a clean `OperationCanceledException`.
- `SmtpMailKitSender.SendAsync` uses MailKit's token-accepting `ConnectAsync`/`AuthenticateAsync`/`SendAsync`/`DisconnectAsync` overloads throughout, so cancellation behaves as expected at each stage.
- Each call to `SendAsync` on either sender opens a fresh `SmtpClient`/connection and disposes/disconnects it at the end of the call — connections are not pooled or reused across calls.
- `AddSmtpMail`/`AddSmtpMailKit` invoke the options `Action` once at registration time (not per resolution), so all `ISmtpMailSender` instances produced by a given registration share the same `Host`/`Port`/credentials/`UseSsl`. Registration is `AddTransient`, so a new sender instance is created per resolution, but from the same fixed options.
- `SmtpConnection.Host`/`Port` and `SmtpMailKitSender.UserName`/`Password` are `protected set` — settable only via the constructor by design; only `UseSsl` can be changed after construction (as in the DI extensions and the direct-instantiation examples above).
