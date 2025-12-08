## Context
- Current `SharedAssemblyCatalog` is built from assemblies already loaded in the default context using domain metadata and a small baked-in allowlist. Hosts cannot extend the shared set via configuration, and modules cannot provide hints for expected shared dependencies.
- Missing or misdeclared shared assemblies cause duplicate loads, UI resource mismatches, and type identity errors. Diagnostics are limited to warnings from `ModuleLoadContext` without a consolidated surface or telemetry to management.
- Pending change `update-runtime-module-lifecycle` already introduces shared assembly resolution from domain metadata; this change layers configuration/hints and diagnostics on top without altering the domain attribute contract.

## Goals / Non-Goals
- Goals: add configuration and manifest hint inputs for the shared catalog; surface diagnostics (API/UI) that show shared entries and mismatches; emit management-facing telemetry on shared resolution failures.
- Non-Goals: redesign AssemblyLoadContext mechanics, introduce new transport for management (reuse existing diagnostics channel), or add host-specific UI styling beyond listing/filtering diagnostics.

## Decisions
- Source merge order: domain-declared Shared assemblies are authoritative; host config (appsettings/env) extends the shared set; manifest `sharedAssemblyHints` are additive per module load. Conflicts where a config/hint marks an assembly shared but domain metadata says Module are allowed for resolution but must emit diagnostics.
- Configuration shape: add a host-level list `SharedAssemblies` (e.g., `Modulus:Runtime:SharedAssemblies`) for simple names; support environment variable binding. Manifest adds `sharedAssemblyHints: string[]` in `manifest.json`. No host toggle to disable manifest hints (always honored), so diagnostics must make any misuse visible.
- Diagnostics surface: provide a runtime/management API that returns shared assemblies with their sources (domain/config/manifest), effective domain, and requesting modules. Capture mismatched shared requests (Module-declared or missing) with module id and reason. Host/management UI renders these lists for operators.
- Failure telemetry: reuse the existing management diagnostics channel/logs for structured events (module id, assembly name, source: domain/config/manifest, reason) when shared resolution fails; no new endpoint unless future scale requires it.

## Risks / Trade-offs
- Allowing config/hints to mark Module-domain assemblies as shared can hide declaration errors; mitigated by mandatory diagnostics entries and surfacing in UI/telemetry.
- Manifest hints could be abused; validation should cap hint count/length and only accept simple names to reduce attack surface.
- Additional config surface increases support burden; defaults remain unchanged when no config/hints are provided.

## Open Questions
- None at this time.

