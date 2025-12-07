## ADDED Requirements

### Requirement: Runtime module lifecycle and cleanup
Runtime-loaded modules MUST execute full lifecycle with module-scoped DI and clean teardown.

#### Scenario: Load executes lifecycle with module services
- **WHEN** a module is loaded at runtime with a valid manifest and supported host
- **THEN** the loader builds a module ServiceCollection/ServiceProvider
- **AND** instantiates all `IModule` types in the package
- **AND** runs Pre/Configure/PostConfigureServices followed by OnApplicationInitializationAsync
- **AND** registers module UI/menu contributions and marks the module active

#### Scenario: Unload calls shutdown and removes registrations
- **WHEN** a loaded module is unloaded
- **THEN** OnApplicationShutdownAsync is invoked in reverse order
- **AND** module menus/navigation/views/services registered during load are deregistered
- **AND** the module ServiceProvider is disposed and its AssemblyLoadContext is unloaded

#### Scenario: System modules protected from unload
- **WHEN** an unload is requested for a system module
- **THEN** the operation is rejected with a clear error

### Requirement: Manifest validation for host, deps, and integrity
Manifests MUST be validated for required fields, host compatibility, dependency semantics, and integrity.

#### Scenario: Unsupported host is rejected
- **WHEN** the manifest SupportedHosts does not include the current host
- **THEN** loading fails with a diagnostic explaining the host mismatch

#### Scenario: Dependency or version constraint failure blocks load
- **WHEN** a declared dependency is missing or its version does not satisfy the SemVer range
- **THEN** loading fails with a diagnostic naming the missing/invalid dependency

#### Scenario: Integrity checks enforced when provided
- **WHEN** manifest or assembly hashes/signatures are present
- **THEN** the loader verifies them and rejects the module on mismatch

### Requirement: Unified dependency graph with topo ordering
Module initialization order MUST use a unified dependency graph from manifest Dependencies and DependsOn attributes, with validation.

#### Scenario: Missing or cyclic dependency fails fast
- **WHEN** the graph contains a missing module or a cycle
- **THEN** loading/initialization is blocked and the error identifies the offending modules

#### Scenario: Modules initialize in dependency order
- **WHEN** modules have dependencies
- **THEN** initialization runs in topological order honoring both manifest and DependsOn links

### Requirement: Shared assembly resolution from domain metadata
Shared assembly allowlist MUST come from assembly-domain metadata/config, with diagnostics for mismatches.

#### Scenario: Shared assemblies resolved via domain metadata
- **WHEN** a module requests an assembly marked Shared by assembly-domain metadata
- **THEN** it is resolved from the host shared context instead of a private copy

#### Scenario: Misdeclared shared/module assembly surfaces diagnostics
- **WHEN** a Module-domain assembly is requested from the shared list or vice versa
- **THEN** the loader emits a diagnostic to help correct the domain assignment


