# Change: Use DependsOn as the sole dependency source

## Why
- Manifest and runtime currently mix manifest-declared dependencies with `[DependsOn]` attributes, producing divergent graphs, inconsistent load order, and scattered diagnostics.
- A single dependency source simplifies validation, improves determinism, and avoids conflicting declarations.

## What Changes
- Build module dependency graphs exclusively from `[DependsOn]` attributes on module assemblies; ignore manifest dependency entries.
- Introduce a unified dependency graph builder used by install, enable, and load flows, with shared detection for missing dependencies, cycles, and version/range mismatches.
- Perform install-time validation by reading `[DependsOn]` metadata from module assemblies inside the package (without loading into the default ALC) and reject invalid modules.
- Standardize dependency diagnostics (missing, cycle, version) and persist them in module state/logs; block enabling/loading when the graph is invalid.

## Impact
- Affected specs: `runtime`
- Affected code: module installer/loader pipeline, dependency graph builder, manifest processing (dependency fields ignored)
