## 1. Implementation
- [ ] 1.1 Add host configuration binding (appsettings/env) for shared assemblies and merge into SharedAssemblyCatalog with validation and precedence over domain metadata.
- [ ] 1.2 Add manifest-level shared assembly hints, validator updates, and catalog merge behavior scoped per module load.
- [ ] 1.3 Expose diagnostics API to list shared assemblies with sources and mismatched shared requests; add management/host UI view.
- [ ] 1.4 Emit shared assembly resolution failure events to the management diagnostics channel with module/assembly/reason payloads.
- [ ] 1.5 Cover new behaviors with unit/integration tests for config merge, manifest hints, diagnostics surface, and failure reporting.

## 2. Validation
- [ ] 2.1 `openspec validate add-shared-assembly-catalog-observability --strict`

