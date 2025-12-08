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

