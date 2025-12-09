## Context
- Runtime hot-load/unload must execute module lifecycle and integrate UI/service registrations safely.
- Manifest validation needs host/dependency/semver/integrity checks.
- Dependency ordering must merge manifest deps with DependsOn and detect cycles/missing modules.
- Shared assembly allowlist should follow assembly-domain metadata instead of hardcoded names.

## Decisions
- Create module-scoped ServiceCollection/ServiceProvider per runtime load; run Pre/Configure/PostConfigureServices then OnApplicationInitializationAsync; register menus/views immediately; store handle for cleanup.
- On unload, invoke OnApplicationShutdownAsync, unregister menus/navigation, dispose module provider/scope, remove module manager/context entry, then unload ALC; guard system modules.
- Extend manifest validator: required fields, supportedHosts match host, semver parse + VersionRange validation, dependency existence, optional assembly hashes + signature.
- Build unified dependency graph combining manifest deps (id+version) and DependsOn attributes; topological sort with cycle/missing diagnostics, surface errors to UI/logs.
- Shared allowlist sourced from AssemblyDomainInfo metadata over default context assemblies plus optional config; ModuleLoadContext consults catalog and logs diagnostics when Module-domain assemblies are requested as shared.

## Risks / Trade-offs
- Additional DI scopes per module increase memory; mitigated by disposal on unload.
- Semver dependency validation adds NuGet.Versioning dependency; chosen for correctness over bespoke parsing.
- Menu/navigation deregistration requires registry changes; ensure backward compatibility with additive remove APIs.

## Migration Plan
- Add shared allowlist catalog and manifest validation enhancements.
- Update loader/unloader to use module handles and dependency resolver.
- Update navigation/menu registries to support deregistration.
- Run tests covering load/unload, validation, and dependency ordering.


