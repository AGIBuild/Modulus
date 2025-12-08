## ADDED Requirements

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
