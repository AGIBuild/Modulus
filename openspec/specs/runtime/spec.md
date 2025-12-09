# runtime Specification

## Purpose
TBD - created by archiving change database-driven-modules. Update Purpose after archive.
## Requirements
### Requirement: Hybrid Module/Component Runtime
The runtime SHALL load modules (ModulusModule) as deployable units and resolve components (ModulusComponent) as code units with dependency ordering.

#### Scenario: Module load succeeds
- **WHEN** a module is enabled and its manifest exists
- **THEN** the runtime loads its assemblies in an isolated ALC (by default)
- **AND** discovers all `ModulusComponent` types
- **AND** builds a dependency graph from `[DependsOn]` (including cross-module dependencies)
- **AND** initializes components in topological order (`ConfigureServices` then `OnApplicationInitialization`).

#### Scenario: Missing files are flagged
- **WHEN** a module manifest file is missing at startup
- **THEN** the module state is set to `MissingFiles`
- **AND** the module is skipped from the load list.

### Requirement: Menu Projection
Menu entries SHALL be projected to the database at install/update time and read from the database at render time.

#### Scenario: Install or update module
- **WHEN** a module is installed or updated
- **THEN** the installer scans components for `[Menu]` (host-aware)
- **AND** writes menu rows with ids like `{ModuleId}.{MenuId}` into `Sys_Menus`
- **AND** replaces any existing menus for that module (bulk upsert).

#### Scenario: Render menus
- **WHEN** the shell renders navigation
- **THEN** it queries `Sys_Menus` joined with enabled modules
- **AND** does not require reflection at render time.

### Requirement: Detail Content Fallback
Module detail pages SHALL prefer README content and fall back to manifest description.

#### Scenario: README available
- **WHEN** `README.md` exists in the module folder
- **THEN** its Markdown content is used for the detail view.

#### Scenario: README missing
- **WHEN** no `README.md` exists
- **AND** `manifest.description` is present
- **THEN** the manifest description is shown.

#### Scenario: No content available
- **WHEN** neither README nor manifest description exists
- **THEN** show "No description provided."

### Requirement: Module State Management
Module state SHALL be persisted and enforced at startup.

#### Scenario: Startup integrity
- **WHEN** the application starts
- **THEN** the system migrates the DB
- **AND** seeds system modules (install/upgrade if missing or outdated)
- **AND** marks missing manifests as `MissingFiles`
- **AND** only modules with `State=Ready` and `IsEnabled=true` are loaded.

### Requirement: Standardized module lifecycle states and lazy activation
Runtime modules SHALL follow a Loaded/Active/Error state machine, gate initialization on host binding, and lazily activate detail views.

#### Scenario: Host not bound keeps module Loaded
- **WHEN** a module is discovered/installed and the host is not yet bound
- **THEN** the runtime loads manifest/menu metadata only
- **AND** marks the module state as `Loaded`
- **AND** defers module initialization, services, and detail view creation.

#### Scenario: Host binding activates eligible modules
- **WHEN** the host binds
- **THEN** the runtime initializes all Ready+enabled modules using the lifecycle pipeline
- **AND** updates their state to `Active` upon success
- **AND** records diagnostics for any module that fails to activate.

#### Scenario: Lazy detail load on first navigation
- **WHEN** a user first navigates to a module’s detail view
- **THEN** the runtime asynchronously loads and renders the module detail/page
- **AND** supports cancellation/timeout for the detail load
- **AND** surfaces errors and sets the module state to `Error` if detail load fails.

#### Scenario: Initialization failure transitions to Error
- **WHEN** module initialization or detail load fails
- **THEN** the module state becomes `Error`
- **AND** diagnostics are recorded for recovery
- **AND** the module is not reused until a retry/repair is attempted.

### Requirement: Unified unload and cleanup pipeline
Runtime modules MUST expose and execute a standardized cleanup path on unload/disable to remove registrations and release resources.

#### Scenario: Unload cleans registrations and resources
- **WHEN** a non-system module is unloaded or disabled
- **THEN** the runtime invokes module-provided deregistration hooks for menus/navigation/messages/subscriptions
- **AND** disposes the module-scoped ServiceProvider, caches, and other scoped resources
- **AND** removes the module’s UI/navigation projections from the host
- **AND** unloads the module AssemblyLoadContext and returns the module to the `Loaded` state (metadata only).

#### Scenario: System modules protected from unload
- **WHEN** an unload is requested for a system module
- **THEN** the runtime rejects the request with a clear diagnostic.

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

