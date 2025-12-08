## ADDED Requirements

### Requirement: Install-time host-aware manifest validation
Module installation SHALL validate host compatibility, UI assemblies for the target host, and dependency version ranges before a module can be enabled or loaded.

#### Scenario: Unsupported host blocks install
- **WHEN** the installer runs with `hostType` X
- **AND** `supportedHosts` does not include X
- **THEN** validation fails with a diagnostic explaining the mismatch
- **AND** the module is not marked Ready or enabled.

#### Scenario: Missing host UI assemblies blocks install
- **WHEN** the installer runs with `hostType` X
- **AND** `uiAssemblies` is missing or empty for X
- **THEN** validation fails with a diagnostic naming the host and missing UI assemblies
- **AND** the module remains disabled/not Ready.

#### Scenario: Invalid dependency ranges are rejected
- **WHEN** the manifest lists dependencies with an invalid semantic version range
- **THEN** validation fails with a diagnostic citing the offending dependency id and range
- **AND** the module is not installed as Ready.

#### Scenario: Valid manifest becomes installable
- **WHEN** supportedHosts includes the current host
- **AND** uiAssemblies provides entries for that host
- **AND** all dependency ranges parse successfully
- **THEN** installation records the module as Ready
- **AND** the module may be enabled/loaded.
