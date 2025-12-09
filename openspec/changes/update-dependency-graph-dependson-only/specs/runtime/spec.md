## ADDED Requirements
### Requirement: Dependency graph from DependsOn attributes
The runtime MUST build and validate module dependency graphs solely from `[DependsOn]` attributes declared on module assemblies; manifest-declared dependency entries MUST be ignored for ordering and validation.

#### Scenario: Install rejects missing dependency
- **WHEN** the installer processes a module whose `[DependsOn]` references a module id not present in the install set or already available modules
- **THEN** installation fails with a missing-dependency diagnostic naming the missing module id

#### Scenario: Install rejects dependency cycles
- **WHEN** `[DependsOn]` declarations form a cycle
- **THEN** installation rejects the module set and emits a cycle diagnostic listing the participating modules

#### Scenario: Install rejects unmet version or range
- **WHEN** a `[DependsOn]` declaration includes a version or range that is not satisfied by the available module version
- **THEN** installation fails with a version-mismatch diagnostic identifying the expected range and the available version

#### Scenario: Runtime load order uses attribute graph
- **WHEN** the runtime computes module load or enable order
- **THEN** it uses a topological order derived from `[DependsOn]` only and fails fast with diagnostics if the graph is invalid
