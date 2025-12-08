## ADDED Requirements

### Requirement: Shared assembly catalog uses configuration and manifest hints
The shared assembly catalog SHALL merge domain metadata with host configuration entries and module manifest hints to avoid duplicate loads and resource mismatches.

#### Scenario: Host configuration extends shared catalog
- **WHEN** the host provides shared assembly names via configuration (appsettings/environment) before modules load
- **THEN** the runtime merges those names with assemblies declared Shared by domain metadata into the shared catalog
- **AND** each merged entry records its source as `config` for diagnostics
- **AND** invalid or empty names are rejected with a diagnostic.

#### Scenario: Manifest declares shared assembly hints
- **WHEN** a module manifest includes `sharedAssemblyHints` listing assemblies expected to be shared
- **AND** the host has those assemblies loaded in the default context
- **THEN** the loader adds the hinted assemblies to the shared catalog for that module load
- **AND** records the source as `manifest` with the module id for diagnostics
- **AND** emits a diagnostic when a hinted assembly is declared Module-domain.

### Requirement: Shared assembly diagnostics surface shared entries and mismatches
The runtime SHALL expose diagnostics that list shared assemblies with their sources and capture mismatched shared requests.

#### Scenario: Diagnostics API lists shared assemblies with sources
- **WHEN** diagnostics are requested via the runtime/management API
- **THEN** the system returns assemblies treated as shared with their sources (domain attribute, host config, manifest hint) and effective domain.

#### Scenario: Mismatched shared requests are surfaced
- **WHEN** a module requests an assembly as shared but the assembly is absent or declared Module-domain
- **THEN** the runtime records a diagnostic entry including module id, assembly name, and reason
- **AND** the management/host UI can display these unresolved or mismatched requests.

### Requirement: Shared resolution failures are reported to management
Shared assembly resolution failures MUST be returned to callers and reported to the management diagnostics channel.

#### Scenario: Load failure due to shared resolution emits telemetry
- **WHEN** module loading fails because a shared assembly cannot be resolved or conflicts with its declared domain
- **THEN** the loader returns the failure to the caller
- **AND** emits a structured diagnostic event to the management channel containing module id, assembly name, source (domain/config/manifest), and failure reason.

