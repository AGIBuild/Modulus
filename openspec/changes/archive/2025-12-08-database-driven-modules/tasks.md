# Tasks

- [x] **Infrastructure Setup**
    - [x] Add `Modulus.Infrastructure.Data` project (Class Library).
    - [x] Add packages: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`.
    - [x] Define `ModulusDbContext`, `ModuleEntity`, `MenuEntity`.
    - [x] Create initial EF Migration.

- [x] **Core Logic Refactor**
    - [x] Implement `IModuleRepository` and `IMenuRepository`.
    - [x] Create `ModuleInstallerService`:
        - [x] Logic to Scan Assembly Attributes -> MenuEntities.
        - [x] Logic to Write Manifest -> ModuleEntity.
    - [x] Create `SystemModuleSeeder`:
        - [x] List of built-in modules.
        - [x] Logic to trigger `ModuleInstallerService` on startup.

- [x] **Runtime Integration**
    - [x] Update `ModulusApplication`:
        - [x] Remove directory scanner.
        - [x] Call `SystemModuleSeeder.EnsureSeededAsync()`.
        - [x] Call `IntegrityChecker.CheckAsync()`.
    - [x] Update `ModuleLoader`:
        - [x] Accept `List<ModuleEntity>` instead of scanning paths (via `ModulusApplicationFactory` logic).
    - [x] Update `ShellViewModel` (Avalonia/Blazor):
        - [x] Load menus from `IMenuRepository` (Via `ModulusApplication` pushing to `IMenuRegistry` which Shell consumes).

- [x] **UI Updates**
    - [x] Update Module Management Page:
        - [x] Show list from DB.
        - [x] Handle "Yellow Warning" state (Missing Files).
        - [x] Implement "Remove" (Delete DB record + Clean folder).
    - [x] Add "Import Module" button (for Devs).
