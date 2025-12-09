# Change: Enforce host-aware manifest validation at install time

## Why
- Module installation currently skips host-specific validation; incompatible modules (unsupported host, missing UI assemblies, bad dependency ranges) slip through and fail only at runtime.
- Installation should block or flag invalid modules before they can be enabled or loaded.

## What Changes
- Pass hostType through installer flows and enforce `supportedHosts` plus host-specific `uiAssemblies` at install time.
- Validate dependency version ranges during install; reject or flag modules with invalid semver ranges.
- Persist validation errors to module state and surface diagnostics to logs/UI so invalid modules are not enabled or loaded.

## Impact
- Affected specs: runtime
- Affected code: ModuleInstallerService, manifest validation pipeline, module state handling, installer diagnostics/UX
