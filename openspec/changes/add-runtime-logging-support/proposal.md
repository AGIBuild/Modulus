# Change: Runtime logging with console and file sinks

## Why
- Hosts and runtime rely on ad-hoc console logging or `NullLogger`, leaving module discovery/load issues without diagnostics once the process exits.
- There is no file-based runtime log, so production incidents cannot be reconstructed from user machines.
- Logging configuration is not centralized or host-aware, and log entries lack host/module context for troubleshooting.

## What Changes
- Introduce a unified logging configuration (console + file) driven by `appsettings`/environment with defaults for levels, retention, and paths.
- Enable structured file logging with rolling retention under a predictable host-specific logs folder, while keeping console logging configurable (default on for development).
- Propagate a single `ILoggerFactory` into runtime/module contexts and enrich entries with host type and module identity.
- Add lifecycle logging for module discovery/load/init/shutdown and manifest validation so failures surface in both console and file sinks.

## Impact
- Affected specs: logging (new)
- Affected code: Modulus.Core runtime bootstrap (ModulusApplicationFactory, ModuleLoader/Manager/LoadContext), host startup (Avalonia/Blazor builders), host configuration files

