# Lightsoft.SmtpMail

SMTP email-sending implementations behind a single shared contract, `ISmtpMailSender`. The package ships two senders — one built on the legacy `System.Net.Mail.SmtpClient` (no authentication) and one built on MailKit (with authentication) — plus optional `IServiceCollection` registration helpers for each.

- **NuGet package id / assembly name:** `Lightsoft.SmtpMail` (no explicit `PackageId`, so it defaults to `AssemblyName`)
- **Root namespace:** `Light.SmtpMail` — DI extensions live under `Light.Extensions.DependencyInjection`
- **Target framework:** netstandard2.1
- **Dependencies:** `MailKit` (4.17.0), `MimeKit` (4.17.0), `Microsoft.Extensions.DependencyInjection.Abstractions`. `ProjectReference` to `Mail.Contracts` (`Lightsoft.Mail.Contracts`, root namespace `Light.Mail`), which supplies `MailFrom`, `MailMessage`, and `MailAttachment`.

## What's in this package

| Type | Namespace | Purpose |
|---|---|---|
| `ISmtpMailSender` | `Light.SmtpMail` | Shared contract: `Task SendAsync(MailFrom from, MailMessage mail, CancellationToken cancellationToken = default)`. Implemented by both senders below. |
| `SmtpConnection` | `Light.SmtpMail` | Abstract base exposing `Host` (protected set), `Port` (protected set), and `UseSsl` (public set) — common connection state for both senders. |
| `SmtpNetMailSender` | `Light.SmtpMail` | `SmtpConnection` + `ISmtpMailSender` implementation using the built-in `System.Net.Mail.SmtpClient`. No authentication support. Renamed this session from `SmtpMail`, which collided with the containing `Light.SmtpMail` namespace. |
| `SmtpMailKit` | `Light.SmtpMail` | `SmtpConnection` + `ISmtpMailSender` implementation using MailKit's `SmtpClient`. Supports authentication (`UserName`/`Password`, both public get / protected set). |
| `SmtpMailOptions` | `Light.SmtpMail` | Options bag for `AddSmtpMail`: `Host`, `Port` (default `25`), `UseSsl` (default `false`). |
| `SmtpMailKitOptions` | `Light.SmtpMail` | Options bag for `AddSmtpMailKit`: `Host`, `Port` (default `587`), `UserName`, `Password`, `UseSsl` (default `false`). |
| `ServiceCollectionExtensions` | `Light.Extensions.DependencyInjection` | `AddSmtpMail(Action<SmtpMailOptions>)` and `AddSmtpMailKit(Action<SmtpMailKitOptions>)` — both register `ISmtpMailSender` as `AddTransient`. |

## `SmtpNetMailSender` vs `SmtpMailKit` — which one to use

- **`SmtpNetMailSender`** wraps `System.Net.Mail.SmtpClient` directly. It has no way to authenticate — it's only suitable for simple/local relays that accept anonymous connections (e.g. an internal relay, a dev SMTP catcher, IIS SMTP). Because the legacy `SmtpClient` has no cancellation-token-accepting `SendMailAsync` overload on netstandard2.1, cancellation is wired up by registering `smtpClient.SendAsyncCancel` against the `CancellationToken` — see [Notes](#notes) for the resulting behavior.
- **`SmtpMailKit`** wraps MailKit's `SmtpClient` and always authenticates (`ConnectAsync` → `AuthenticateAsync` → `SendAsync` → `DisconnectAsync`), all with full native `CancellationToken` support. This is the generally recommended choice for real mail providers (SendGrid, Ethereal, Gmail SMTP, Office 365, etc.) that require authenticated, TLS-capable SMTP.

In short: reach for `SmtpMailKit` unless you specifically need to talk to an unauthenticated relay, in which case `SmtpNetMailSender` is the lighter-weight option.

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
            new MailFrom("no-reply@example.com", "Example App"),
            new MailMessage
            {
                Subject = "Welcome",
                Content = "<p>Thanks for signing up.</p>",
                Recipients = ["user@example.com"]
            },
            cancellationToken);
}
```

Only register one of `AddSmtpMail`/`AddSmtpMailKit` per `ISmtpMailSender` consumer — calling both registers two implementations against the same service type, and the last one registered wins for a plain `ISmtpMailSender` injection.

### Direct instantiation (no DI)

Both senders can also be constructed directly, e.g. from a controller or background job:

```csharp
var from = new MailFrom("leslie.bailey@ethereal.email");

var message = new MailMessage
{
    Subject = "Test " + DateTime.Now,
    Content = "Hello, this is a test mail",
    Recipients = ["test@yopmail.com"]
};

var smtpClient = new SmtpMailKit("smtp.ethereal.email", "jermain.torphy@ethereal.email", "GHMdV12nF7zfFhqG7Z")
{
    UseSsl = false
};

await smtpClient.SendAsync(from, message);
```

`SmtpNetMailSender` is constructed the same way, minus credentials:

```csharp
var smtpClient = new SmtpNetMailSender("localhost", 25) { UseSsl = false };
await smtpClient.SendAsync(from, message);
```

### Attachments

Both senders read `MailMessage.Attachments` (`List<MailAttachment>?`, from `Mail.Contracts`) when present and attach each one:

```csharp
message.Attachments = [ new MailAttachment("report.pdf", fileBytes) ];
```

`SmtpNetMailSender` wraps each `MailAttachment.FileToBytes` in a `MemoryStream` and adds it as a `System.Net.Mail.Attachment`; `SmtpMailKit` adds each one to a MailKit `BodyBuilder.Attachments` collection. `CcRecipients`/`BccRecipients`/`Attachments` are all optional (`null`-safe — skipped when `null`); `Recipients` is not — it must be a non-null, non-empty list or `SendAsync` throws when iterating it.

## Notes

- Both senders always send an HTML body: `SmtpNetMailSender` sets `IsBodyHtml = true`, and `SmtpMailKit` sets `HtmlBody` on the `BodyBuilder`. Plain-text bodies aren't supported by either sender.
- `SmtpMailKit` always authenticates — there is no anonymous-send path on that class. If you need an unauthenticated relay, use `SmtpNetMailSender` instead.
- `SmtpNetMailSender`'s cancellation support is indirect: it registers `SmtpClient.SendAsyncCancel` against the token rather than passing the token into a native async overload (none exists for `SmtpClient.SendMailAsync` on netstandard2.1). Cancelling aborts the in-flight send, but the exception surfaced comes from `SmtpClient` itself and is not guaranteed to be a clean `OperationCanceledException`.
- `SmtpMailKit.SendAsync` uses `MailKit`'s token-accepting `ConnectAsync`/`AuthenticateAsync`/`SendAsync`/`DisconnectAsync` overloads throughout, so cancellation behaves as expected at each stage.
- Each call to `SendAsync` on either sender opens a fresh `SmtpClient`/connection and disposes/disconnects it at the end of the call — connections are not pooled or reused across calls.
- `AddSmtpMail`/`AddSmtpMailKit` invoke the options `Action` once at registration time (not per resolution), so all `ISmtpMailSender` instances produced by a given registration share the same `Host`/`Port`/credentials/`UseSsl`. Registration is `AddTransient`, so a new sender instance is created per resolution, but from the same fixed options.
- `SmtpConnection.Host`/`Port` and `SmtpMailKit.UserName`/`Password` are `protected set` — settable only via the constructor by design; only `UseSsl` can be changed after construction (as in the DI extensions and the direct-instantiation examples above).
