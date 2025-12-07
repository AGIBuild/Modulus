# Change: Update runtime module lifecycle & validation

## Why
- Runtime-loaded modules skip DI/lifecycle and leave stale UI/service registrations.
- Manifest validation misses host compatibility, dependency semantics, and integrity checks.
- Dependency ordering ignores manifest deps and cycle/missing detection.
- Shared assembly allowlist is hardcoded, risking duplicate loads and type mismatches.

## What Changes
- Add module-scoped DI + lifecycle execution (Pre/Configure/Post + init/shutdown) with tracked handles and cleanup on unload.
- Strengthen manifest validation for required fields, supported hosts, semver dependencies, hashes, and signature.
- Build unified dependency graph (manifest deps + DependsOn) with topo sort and diagnostics.
- Drive shared assembly allowlist from assembly-domain metadata with runtime diagnostics.

## Impact
- Affected specs: runtime
- Affected code: Modulus.Core runtime loader/manager, manifest validation, load context, host menu/navigation integration


