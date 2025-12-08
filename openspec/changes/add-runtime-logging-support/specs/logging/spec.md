## ADDED Requirements

### Requirement: Logging configuration
The host SHALL expose a shared logging configuration that controls console and file sinks for runtime and modules.

#### Scenario: Default configuration applied
- **WHEN** the host starts without custom logging settings
- **THEN** console logging is enabled at Information level
- **AND** file logging is enabled at Information level
- **AND** the log directory defaults to the host-specific logs folder.

#### Scenario: Environment overrides configuration
- **WHEN** logging settings (levels, enablement, paths) are provided via appsettings or environment variables
- **THEN** the runtime applies the overridden values without code changes
- **AND** the settings flow to all runtime and module loggers.

### Requirement: Console logging
The system SHALL emit console logs for runtime and module events when console logging is enabled.

#### Scenario: Console output on startup
- **WHEN** the host starts with console logging enabled
- **THEN** startup events (database init, module discovery, module load begin/end) are written to STDOUT with timestamp and level.

#### Scenario: Console logging can be disabled
- **WHEN** console logging is disabled via configuration
- **THEN** runtime and module logs stop writing to the console sink.

### Requirement: File runtime logging
The system SHALL write runtime and module logs to a rolling file sink with retention using Serilog (MEL provider + Serilog file sink), avoiding custom file writers.

#### Scenario: File log created with defaults
- **WHEN** the host starts with file logging enabled
- **THEN** a log file is created under the configured logs directory (default `%AppData%/Modulus/Logs/{Host}/` or `$HOME/.modulus/logs/{host}/`)
- **AND** runtime/module log entries are appended with timestamps and levels.

#### Scenario: Rolling and retention enforced
- **WHEN** a log file exceeds the configured size limit or rolling interval
- **THEN** the file is rolled with an incremented or timestamped suffix
- **AND** only the configured number of retained log files is kept.

#### Scenario: Serilog file sink in use
- **WHEN** file logging is enabled
- **THEN** the logging pipeline uses the Serilog MEL provider with the Serilog file sink
- **AND** no custom file writer is used for runtime or module logging.

### Requirement: Log context enrichment
Runtime log entries SHALL include host and module context.

#### Scenario: Host log context
- **WHEN** the host emits a log entry
- **THEN** the entry includes the host type identifier (e.g., Avalonia, Blazor).

#### Scenario: Module log context
- **WHEN** code inside a loaded module emits a log entry
- **THEN** the entry includes the module id and version when available
- **AND** the host type context remains present.

### Requirement: Runtime lifecycle logging coverage
The runtime SHALL log critical lifecycle events for module discovery, validation, load, initialization, and shutdown.

#### Scenario: Module load success
- **WHEN** a module is loaded successfully
- **THEN** an info-level log is written with module id, version, and dependency count.

#### Scenario: Module load failure
- **WHEN** a module fails to load or initialize
- **THEN** an error-level log is written with module id, path, and exception details
- **AND** the failure is persisted to the file sink.

### Requirement: Module logging usage
Modules SHALL use the host-provided logging pipeline without redefining or redirecting logging configuration.

#### Scenario: Module obtains logger
- **WHEN** a module requests `ILogger<T>` or `ILoggerFactory` from DI
- **THEN** it receives the host-configured logging pipeline (console/file Serilog) with host and module context enrichment.

#### Scenario: Module cannot reconfigure logging
- **WHEN** a module attempts to add/replace logging providers or change logging levels/paths
- **THEN** the attempt is ignored or rejected
- **AND** the host-owned logging configuration remains active for all runtime and module logs.

