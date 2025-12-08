## Context
- Hosts call `AddLogging()` with defaults and runtime bootstraps its own `LoggerFactory.Create(builder => builder.AddConsole())`, producing inconsistent providers and no file sink.
- Module providers and runtime loaders often receive `NullLogger`, so module discovery/load errors are invisible once the process exits.
- There is no shared configuration contract for logging, no host/module context enrichment, and no retention policy for operators.

## Goals / Non-Goals
- Goals: unify logging pipeline across hosts/runtime, support configurable console + file sinks with sane defaults, include host/module context in log scopes, and keep retention predictable.
- Non-Goals: remote log shipping/observability (Elastic/App Insights), in-app log viewer UI, or distributed tracing.

## Decisions
- Logging stack: keep `Microsoft.Extensions.Logging` as the abstraction and add Serilog sinks (via `Serilog.Extensions.Logging` + `Serilog.Sinks.File`) for file output—no custom file writers; console uses built-in MEL console.
- Configuration contract: `Logging:Console:{Enabled,Level,Format}` and `Logging:File:{Enabled,Level,Path,RollingInterval,FileSizeLimitMB,RetainedFileCountLimit}` with environment variable overrides supported by default binding.
- Defaults: console enabled at `Information`, file enabled at `Information`, rolling daily or 10 MB size (whichever first), retain last 10 files.
- Paths: default logs folder at `%AppData%/Modulus/Logs/{HostType}/` on Windows and `$HOME/.modulus/logs/{hostType}/` elsewhere; filenames include host + date to avoid clashes across hosts.
- Context enrichment: attach `HostType`, `ModuleId`, and `ModuleVersion` via logging scopes at module entry points; lifecycle log messages use structured properties instead of string concatenation.
- Compatibility: replace ad-hoc `LoggerFactory.Create` usage with the configured factory from DI so modules/host share the same providers and filters.
- Module usage: modules resolve `ILogger<T>`/`ILoggerFactory` from DI (host-owned pipeline) and MUST NOT add providers or change logging configuration; host/runtime owns configuration surface and may ignore/reject module-level reconfiguration attempts.

## Risks / Trade-offs
- Additional Serilog dependencies increase package surface; mitigated by using minimal packages (Serilog.Extensions.Logging + Serilog.Sinks.File).
- File IO and retention can increase disk usage; mitigated by size/time rolling and retention limits with documented defaults.
- Concurrent hosts or multiple instances could contend for the same log file; mitigated by host-specific filenames and optional path override.

## Migration Plan
- Introduce the configuration contract with defaults so existing hosts get console + file logging without code changes beyond wiring the pipeline.
- Update runtime bootstrap to consume the DI-provided `ILoggerFactory` and remove `NullLogger` placeholders.
- Add enrichment helpers/scopes and apply them around module discovery/load/init/shutdown.

## Open Questions
- Do we need JSON vs plaintext for file format? (default to JSON for structure unless operators request plaintext.)
- Should we expose a CLI/management endpoint to download logs, or keep out-of-scope for now?

