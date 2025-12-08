## 1. Implementation
- [ ] 1.1 Add `[DependsOn]` metadata reader that extracts module ids and optional versions/ranges without loading assemblies into the default ALC.
- [ ] 1.2 Build a unified dependency graph builder/service consumed by install, enable, and load flows.
- [ ] 1.3 Enforce install-time validation using the `[DependsOn]` graph (missing dependency, cycle, unsatisfied version/range).
- [ ] 1.4 Remove manifest dependency usage from runtime paths; emit warnings if present.
- [ ] 1.5 Persist standardized dependency diagnostics to module state and logs; block enable/load on invalid graphs.
- [ ] 1.6 Add tests covering no-deps, missing-deps, cycles, version/range mismatch, and manifest-dependency-ignored cases.
