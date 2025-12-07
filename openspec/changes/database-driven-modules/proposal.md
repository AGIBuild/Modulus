# Database-Driven Module Lifecycle & Menu System

## Why
Currently, Modulus relies on filesystem scanning to find and load modules. This has several limitations:
1.  **Duplicate Menus**: Inconsistent module IDs between loading and menu registration.
2.  **Performance**: Scanning assemblies and parsing attributes at every startup is slow.
3.  **Fragility**: No persistent state to track user preferences (enabled/disabled) or handle missing files gracefully.
4.  **Versioning**: Hard to manage system module updates vs. user modules.

## What
We will transition from a **Runtime-Scanning** architecture to a **State-Driven** architecture backed by a local database (SQLite + EF Core).

### Key Changes
1.  **Persistence Layer**: Introduce `Sys_Modules` and `Sys_Menus` tables.
2.  **Seeding & Migration**: Auto-register system modules on startup/update.
3.  **Install/Update Pipeline**:
    *   **Install**: Extract -> Load (Temp Context) -> Scan Attributes -> Write DB (Modules & Menus).
    *   **Runtime**: Read DB -> Load Assemblies (No Scanning).
4.  **Menu System**:
    *   **Write-Time**: Parse `[Menu]` attributes during installation.
    *   **Read-Time**: Query `Sys_Menus` to render UI.
5.  **Self-Healing**: Detect missing files on startup, mark as "Warning", allow clean removal.

## Impact
*   **Performance**: Faster startup (menu rendering doesn't wait for modules).
*   **Stability**: Zero duplicate menus; atomic installation.
*   **UX**: Users can manage (enable/disable/remove) modules; "Yellow Warning" for broken ones.
*   **Dev**: Keep using convenient `[Menu]` attributes.

## Migration
Existing modules will need to be "imported" into the database on the first run of the new version.

