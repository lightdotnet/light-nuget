# TODO — Integration Services

Open follow-ups for the `IntegrationServices` solution (`IntegrationServices.slnx`), covering
`ActiveDirectory`, `Caching`, `FileGenerator`, `Graph`, `Mail.Contracts`, `Serilog`, `SmtpMail`
(`src/`), `tests/UnitTests`, and the `samples/WebApi` sample. See each project's own `README.md`
for current, user-facing documentation.

## Found while writing per-project READMEs (2026-07-29)

- [ ] **`samples/WebApi/appsettings.json` configures the Elasticsearch Serilog sink under the key `"ElasticsearchAsync1"`, but `WriteToOptions`/`SerilogOptionsExtensions` does an exact string match on `"ElasticsearchAsync"`.**
  The trailing `1` means the sink silently never activates — no error, it just doesn't log to Elasticsearch. Fix by correcting the config key (or making the lookup less brittle).
- [ ] **`samples/WebApi`'s `GraphController` depends on `IGraphMailService`/`IGraphTeams`, but `Program.cs` has `AddMicrosoftGraph(...)` commented out.**
  Hitting any `GraphController` endpoint will throw a DI resolution error as the sample stands today. Either uncomment `AddMicrosoftGraph` with placeholder config (it needs real Azure AD credentials to actually work) or remove/guard the controller so the sample doesn't 500 on startup-resolvable DI.
- [ ] **Security note, not a bug:** `samples/WebApi/Controllers/MailController.cs` hardcodes real Ethereal test-SMTP credentials in source, and `appsettings.json`/`appsettings.Development.json` commit plaintext Gmail SMTP, Redis, and Elasticsearch credentials. Ethereal is a disposable test-inbox service so low real risk, but worth moving to user-secrets/environment variables so this pattern isn't copy-pasted into a real consumer project.
