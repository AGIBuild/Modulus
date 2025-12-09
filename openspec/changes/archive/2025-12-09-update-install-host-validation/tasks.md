## 1. Implementation
- [x] 1.1 Propagate hostType into installer validation paths and enforce `supportedHosts`.
- [x] 1.2 Require host-specific `uiAssemblies` at install time; fail or flag when missing/empty.
- [x] 1.3 Validate dependency version ranges during install; record diagnostics and block enable/load on invalid manifests.
- [x] 1.4 Persist validation outcome to module state and surface errors to logs/UI.
- [x] 1.5 Add tests covering install-time host/UI/dependency validation and state updates.
