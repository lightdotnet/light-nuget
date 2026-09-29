# TODO — Plugins

Open follow-ups for the `Plugins` solution (`Plugins.slnx`), covering
`ActiveDirectory`, `FileGenerator`, `Graph`, `Serilog`, `SmtpMail`
(`src/`), `tests/UnitTests`, and the `samples/WebApi` sample. See each project's own `README.md`
for current, user-facing documentation.

## Found while writing per-project READMEs (2026-07-29)

- [ ] **`samples/WebApi/appsettings.json` configures the Elasticsearch Serilog sink under the key `"ElasticsearchAsync1"`, but `WriteToOptions`/`SerilogOptionsExtensions` does an exact string match on `"ElasticsearchAsync"`.**
  The trailing `1` means the sink silently never activates — no error, it just doesn't log to Elasticsearch. Fix by correcting the config key (or making the lookup less brittle).
