# Change: Shared assembly catalog configuration and observability

## Why
- Shared assembly allowlist is hardcoded to domain metadata, so hosts cannot extend it via configuration and modules cannot declare expected shared dependencies, leading to duplicated loads and UI resource mismatches.
- There is no diagnostics surface to show which assemblies are shared (and why) versus which were incorrectly requested as shared, making type mismatch debugging difficult.
- Load failures caused by shared assembly resolution do not flow back to management, delaying triage.

## What Changes
- Allow `SharedAssemblyCatalog` to merge shared entries from host configuration (appsettings/env) and module manifest hints, with precedence and validation against domain metadata.
- Expose diagnostics API/UI that lists assemblies treated as shared (including source: domain attribute, host config, manifest hint) and records mismatched shared requests.
- Report shared assembly resolution failures to the management channel so operators see which module/assembly caused the issue.

## Impact
- Affected specs: runtime
- Affected code: SharedAssemblyCatalog build pipeline, ModuleLoadContext diagnostics, manifest schema/validator, host config binding, diagnostics API + management/host UI, telemetry/management reporting

