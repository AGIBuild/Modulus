# Design: Database-Driven Plugin System (Module / Component Split)

## 1) Terminology & Layers
- **ModulusModule** (模块，交付单元)
  - 用户安装/卸载/启用/禁用的顶层对象。
  - 映射：数据库 `Sys_Modules` 记录；文件夹 `App_Data/Modules/{Id}`；`manifest.json`。
  - 边界：版本、权限、隔离（ALC）、可选独立存储/配置。
- **ModulusComponent** (组件，代码单元)
  - C# 类：`public class XxxComponent : ModulusComponent`。
  - 依赖：`[DependsOn]`（可跨模块）。
  - 职责：注册服务、声明菜单/扩展点、参与生命周期。
  - 一个模块可包含多个组件；模块通过 `manifest.entryComponent` 指定入口组件。
- **MenuEntry** 仍投影到 DB (`Sys_Menus`)，来源于组件属性或 manifest。

## 2) Data Model (SQLite)

### `ModuleEntity` (`Sys_Modules`)
| Property | Type | Description |
|---|---|---|
| `Id` | PK, String | `manifest.id` (ModuleId) |
| `Name` | String | Display name |
| `Version` | String | Installed version |
| `Author` | String | Author |
| `Website` | String | Website |
| `Path` | String | Relative path to `manifest.json` |
| `EntryComponent` | String | FQCN of entry component |
| `IsSystem` | Bool | Managed by seeder; not uninstallable |
| `IsEnabled` | Bool | User preference |
| `State` | Enum | `Ready`, `MissingFiles`, `Incompatible` |

### `MenuEntity` (`Sys_Menus`)
| Property | Type | Description |
|---|---|---|
| `Id` | PK, String | Menu id |
| `ModuleId` | FK | Module owner |
| `ParentId` | String? | Nesting |
| `DisplayName`| String |  |
| `Icon` | String |  |
| `Route` | String |  |
| `Order` | Int |  |

## 3) Runtime Workflow (Hybrid Driven)

### A. Startup (Seeding + Integrity)
1. EF Core migrate.  
2. System seeding: ensure required modules exist/updated.  
3. Integrity: mark `MissingFiles` if manifest missing.  
4. **Module load (physical)**: for each enabled `Ready` module, create ALC and load assemblies.  
5. **Component resolution (logical)**: scan assemblies for `ModulusComponent`, build dependency graph via `[DependsOn]`, topo-init (ConfigureServices → Init).

### B. Install/Update (Projection)
1. Extract to `Modules/{Id}`.  
2. Read manifest (includes `entryComponent`).  
3. Isolated scan to find `ModulusComponent` + `[Menu]`.  
4. Upsert `ModuleEntity`; replace menus in `Sys_Menus`.  
5. (Optional) cache component list for diagnostics.

### C. Menu Rendering
- UI reads `Sys_Menus` (join enabled modules). Zero reflection at render time.

## 4) Developer Experience
- **Authoring**: build a Module (package) with one or more Components; mark entry via manifest; use `[DependsOn]`, `[Menu]`, future `[ExtensionPoint]`.  
- **Testing**: import local module (folder/manifest) → projection pipeline.  
- **AI-friendly**: explicit component dependencies; declarative module metadata.

## 5) Technology
- EF Core Sqlite for state.  
- ALC per module (default isolation).  
- Markdown README for detail page (fallback to manifest description).  

