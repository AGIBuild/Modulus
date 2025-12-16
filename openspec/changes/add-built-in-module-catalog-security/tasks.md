# Tasks

## Spec tasks (this change)
- [ ] Add spec deltas for runtime built-in catalog and integrity policies (Prod/Dev)
- [ ] Add spec deltas for module packaging (built-in shipping model, user module anti-rollback)
- [ ] Validate with `openspec validate --strict --no-interactive`

## Implementation tasks (follow-up change / execution)
- [ ] Build-time: generate `BuiltInModuleCatalog` from Host project references
- [ ] Runtime: implement `SyncBuiltInModulesAsync` (2B projection, enforcement, integrity gates)
- [ ] Runtime: system modules load path sourced from catalog, not DB
- [ ] Runtime/Installation: enforce system module cannot disable/uninstall (multi-layer)
- [ ] Runtime: Development policy (env gate) — verify assembly identity (name/strong name) only
- [ ] DB: add user module anti-rollback table and install-time enforcement
- [ ] Tests: add unit/integration tests for
  - [ ] production integrity rejects tampered system modules
  - [ ] development integrity accepts rebuilt system modules with correct identity
  - [ ] system module cannot be disabled/uninstalled
  - [ ] user module anti-rollback rejects downgrade


