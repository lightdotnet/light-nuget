# Lightsoft.Mail.Contracts

Shared mail data-transfer objects for the `Services` solution's mail-sending packages. It defines the message/sender/attachment shapes that `SmtpMail` (`ISmtpMailSender`) and `Graph` (`IGraphMailService`) both accept, so callers can send an email through either transport using the exact same `MailFrom`/`MailMessage` without depending on either implementation package directly.

- **NuGet package id / assembly name:** `Lightsoft.Mail.Contracts` (no explicit `PackageId`, so it defaults to `AssemblyName`)
- **Root namespace:** `Light.Mail` — see [Notes](#notes) for why this differs from the package id
- **Target framework:** netstandard2.1
- **Dependencies:** none. No `PackageReference`s and no `ProjectReference`s — this is a leaf project, sitting at the bottom of the `Services` solution's dependency graph (`SmtpMail` and `Graph` both reference it, not the other way around).

## What's in this package

| Type | Namespace | Purpose |
|---|---|---|
| `MailMessage` | `Light.Mail` | The email body/envelope: recipients, subject, content, optional CC/BCC, optional attachments. |
| `MailFrom` | `Light.Mail` | Sender address and optional display name. |
| `MailAttachment` | `Light.Mail` | A single file attachment as a name + byte array. |

### `MailMessage`

Plain settable-property class, no constructor beyond the implicit default one:

- `List<string> Recipients { get; set; }` — required (declared `= null!`, no default value).
- `string Subject { get; set; }` — required (`= default!`).
- `string Content { get; set; }` — required (`= default!`).
- `List<string>? CcRecipients { get; set; }` — optional, `null` by default.
- `List<string>? BccRecipients { get; set; }` — optional, `null` by default.
- `List<MailAttachment>? Attachments { get; set; }` — optional, `null` by default.

### `MailFrom`

- `MailFrom(string address)` — sets `Address`, leaves `DisplayName` unset.
- `MailFrom(string address, string? displayName)` — sets both.
- `string Address { get; set; }` — declared `= null!`.
- `string? DisplayName { get; set; }`.

### `MailAttachment`

- `MailAttachment(string fileName, byte[] fileToBytes)` — both parameters required; no parameterless constructor.
- `string FileName { get; set; }`.
- `byte[] FileToBytes { get; set; }`.

## Usage

This package only holds the DTOs — you send mail through a consumer package (`SmtpMail` or `Graph`) that accepts them. For example, via `ISmtpMailSender`:

```csharp
using Light.Mail;

var from = new MailFrom("no-reply@example.com", "Example App");

var message = new MailMessage
{
    Recipients = ["someone@example.com"],
    Subject = "Welcome",
    Content = "Hello, thanks for signing up!",
};

message.CcRecipients = ["team@example.com"];
message.Attachments =
[
    new MailAttachment("welcome.pdf", await File.ReadAllBytesAsync("welcome.pdf"))
];

await smtpMailSender.SendAsync(from, message);
```

`IGraphMailService.SendAsync(MailFrom, MailMessage, CancellationToken)` has the identical signature, so the same `from`/`message` instances can be handed to either sender without changes.

## Notes

- The root namespace is `Light.Mail`, not `Light.Mail.Contracts`. This is intentional, not an oversight: because these types are the most-referenced mail types across the `Services` solution (used from `SmtpMail`, `Graph`, and every downstream consumer of either), the shorter namespace was kept deliberately to reduce import noise at call sites. A rename to match the package id was considered during a review pass and rejected to avoid unnecessary churn across the solution.
- `MailMessage.Recipients`, `Subject`, and `Content` are declared non-nullable (`null!`/`default!`) but have no required-member enforcement (no `required` keyword, no validating constructor) — the compiler will not stop you from leaving them unset via object-initializer syntax; only `CcRecipients`, `BccRecipients`, and `Attachments` are genuinely optional (`null` by default).
- `MailAttachment` has no parameterless constructor, so it cannot be built with object-initializer syntax alone (`new MailAttachment { FileName = ..., FileToBytes = ... }` does not compile) — always use the two-argument constructor.
