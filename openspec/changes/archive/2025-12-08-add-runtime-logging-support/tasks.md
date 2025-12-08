## 1. Implementation
- [x] 1.1 Add shared `Logging` configuration for console/file sinks (enable flags, levels, path, rolling/retention) with appsettings + environment overrides in both hosts.
- [x] 1.2 Wire Serilog via `Serilog.Extensions.Logging` + `Serilog.Sinks.File` as the file sink (no custom writers) and initialize a single logging pipeline (console + file) from configuration; pass the configured `ILoggerFactory` into Modulus runtime components instead of ad-hoc factories/null loggers.
- [x] 1.3 Enrich log scopes with host type and module identity and ensure runtime components use these scopes during module discovery, load, initialization, and shutdown.
- [x] 1.4 Add structured lifecycle logs for manifest validation, dependency graph building, module load/init/unload, and host startup/shutdown with clear levels/event IDs.
- [x] 1.5 Add validation/smoke coverage that a log file is created, rolling/retention works, console output remains available when enabled, and the Serilog file sink is active (e.g., host-level test or manual check guidance).
- [x] 1.6 Ensure modules resolve `ILogger<T>`/`ILoggerFactory` from DI (shared host pipeline) and cannot add/replace logging providers or change logging configuration; document or enforce rejection/ignore behavior for module-level reconfiguration attempts.

