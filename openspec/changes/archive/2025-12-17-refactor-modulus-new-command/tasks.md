## 1. Spec
- [x] 1.1 Update `module-template` spec delta for new `modulus new` syntax and removed options
- [x] 1.2 Update `cli-testing` spec delta to use new syntax in `New command creates module` scenario
- [x] 1.3 Run `openspec validate refactor-modulus-new-command --strict`

## 2. Implementation (after approval)
- [x] 2.1 Refactor `src/Modulus.Cli/Commands/NewCommand.cs` to accept `[<template>] -n <name> ...` and `--list`
- [x] 2.2 Remove deleted options and update help text / exit codes
- [x] 2.3 Update CLI integration tests (`CliRunner` + `NewCommandTests`) to new syntax
- [x] 2.4 Update docs command examples (`docs/*`, `README*`) to new syntax


